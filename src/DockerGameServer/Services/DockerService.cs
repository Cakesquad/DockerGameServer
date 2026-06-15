using Docker.DotNet;
using Docker.DotNet.Models;
using DockerGameServer.Docker;
using System.Runtime.CompilerServices;
using System.Text;

namespace DockerGameServer.Services
{
    public sealed record DockerContainerInfo(
        string Id,
        string Name,
        string Image,
        string State,
        string Status,
        IReadOnlyDictionary<string, string> Ports
    );

    public sealed record DockerCreateResult(
        string ContainerId,
        bool Started
    );

    public sealed class DockerService
    {
        private readonly DockerClient _client;

        public DockerService()
        {
            _client = DockerClientFactory.Create();
        }

        public async Task<IReadOnlyList<DockerContainerInfo>> ListContainersAsync(CancellationToken ct = default)
        {
            var containers = await _client.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, ct);

            return containers.Select(c =>
                new DockerContainerInfo(
                    Id: c.ID,
                    Name: c.Names.FirstOrDefault() ?? "",
                    Image: c.Image,
                    State: c.State,
                    Status: c.Status,
                    Ports: c.Ports
                        .GroupBy(p => $"{p.PrivatePort}/{p.Type}")
                        .ToDictionary(
                            g => g.Key,
                            g =>
                            {
                                var port = g.First();
                                return port.PublicPort == 0 ? "" : port.PublicPort.ToString();
                            }
                        )
                )).ToList();
        }

        public async Task<string?> GetContainerIdByNameAsync(string name, CancellationToken ct = default)
        {
            // Docker returns names like "/my-container"
            string Normalize(string n) => n.Trim().TrimStart('/');

            var containers = await _client.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, ct);

            foreach (var c in containers)
            {
                foreach (var n in c.Names)
                {
                    if (Normalize(n).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return c.ID;
                }
            }

            return null;
        }


        public async Task PullImageAsync(string image, string tag = "latest", CancellationToken ct = default)
        {
            var parameters = new ImagesCreateParameters
            {
                FromImage = image,
                Tag = tag
            };

            await _client.Images.CreateImageAsync(
                parameters,
                null,
                new Progress<JSONMessage>(),
                ct
            );
        }

        public async Task<DockerCreateResult> CreateAndStartContainerAsync(
            string image,
            string name,
            IReadOnlyDictionary<string, string> env,
            IReadOnlyDictionary<int, int> ports,
            IReadOnlyList<string> volumeBinds,
            CancellationToken ct = default)
        {
            // Ensure image exists
            await PullImageAsync(image, "latest", ct);

            var envList = env.Select(kv => $"{kv.Key}={kv.Value}").ToList();

            //var portBindings = ports.ToDictionary(
            //    kv => $"{kv.Key}/tcp",
            //    kv => (IList<PortBinding>)new List<PortBinding>
            //    {
            //        new PortBinding { HostPort = kv.Value.ToString() }
            //    });

            var portBindings = ports.ToDictionary(
                kv => $"{kv.Value}/tcp",
                kv => (IList<PortBinding>)new List<PortBinding>
                {
                    new PortBinding { HostPort = kv.Key.ToString() }
                });

            var createParams = new CreateContainerParameters
            {
                //Image = $"{image}:latest",
                Image = $"{image}",
                Name = name,
                Env = envList,
                HostConfig = new HostConfig
                {
                    Binds = volumeBinds.ToList(),
                    PortBindings = portBindings
                }
            };

            var createResponse = await _client.Containers.CreateContainerAsync(createParams, ct);

            if (string.IsNullOrWhiteSpace(createResponse.ID))
                throw new InvalidOperationException("Docker returned empty container ID.");

            var started = await _client.Containers.StartContainerAsync(createResponse.ID, null, ct);

            return new DockerCreateResult(createResponse.ID, started);
        }
        public async Task<string> ExecAsync(string containerId, string[] cmd, CancellationToken ct = default)
        {
            // 1. Create exec instance
            var execCreate = await _client.Exec.ExecCreateContainerAsync(
                containerId,
                new ContainerExecCreateParameters
                {
                    AttachStdout = true,
                    AttachStderr = true,
                    Cmd = cmd
                },
                ct);

            if (string.IsNullOrWhiteSpace(execCreate.ID))
                throw new InvalidOperationException("Failed to create exec instance.");

            // 2. Start exec (MultiplexedStream)
            using var stream = await _client.Exec.StartAndAttachContainerExecAsync(
                execCreate.ID,
                tty: false,
                cancellationToken: ct);

            // 3. Read multiplexed output
            var buffer = new byte[81920];
            using var ms = new MemoryStream();

            while (true)
            {
                var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, ct);

                if (result.Count <= 0)
                    break;
                
                ms.Write(buffer, 0, result.Count);
            }

            // 4. Convert to UTF8 text
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public Task<string> ExecRconCommandAsync(string containerId, string command, CancellationToken ct = default)
        {
            return ExecAsync(containerId, new[] { "rcon-cli", command }, ct);
        }

        public Task<bool> StartContainerAsync(string containerId, CancellationToken ct = default)
            => _client.Containers.StartContainerAsync(containerId, null, ct);

        public Task<bool> StopContainerAsync(string containerId, CancellationToken ct = default)
            => _client.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), ct);

        public Task<bool> RemoveContainerAsync(string containerId, bool force, CancellationToken ct = default)
            => _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters
            {
                Force = force,
                RemoveVolumes = true
            }, ct).ContinueWith(t => t.IsCompletedSuccessfully, ct);

        public async Task<string> GetContainerLogsAsync(string containerId, CancellationToken ct = default)
        {
            var parameters = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Follow = false,
                Timestamps = false
            };

            using var stream = await _client.Containers.GetContainerLogsAsync(containerId, parameters, ct);
            using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync(ct);
        }

        public async IAsyncEnumerable<string> StreamContainerLogsAsync(
            string containerId,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var parameters = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Follow = true,
                Timestamps = false
            };

            using var stream = await _client.Containers.GetContainerLogsAsync(containerId, parameters, ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line is not null)
                    yield return line;
            }
        }
    }
}
