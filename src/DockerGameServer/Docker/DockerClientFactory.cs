using Docker.DotNet;

namespace DockerGameServer.Docker
{
    public static class DockerClientFactory
    {
        public static DockerClient Create()
        {
            if (OperatingSystem.IsWindows())
            {
                return new DockerClientConfiguration(
                    new Uri("npipe://./pipe/docker_engine"))
                    .CreateClient();
            }

            return new DockerClientConfiguration(
                new Uri("unix:///var/run/docker.sock"))
                .CreateClient();
        }
    }

}
