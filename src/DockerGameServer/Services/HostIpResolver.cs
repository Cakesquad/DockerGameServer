using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DockerGameServer.Services
{
    public class HostIpResolver
    {
        public async Task<IPAddress?> GetHostIpAsync()
        {
            var dockerHost = await ResolveDockerHostAsync();
            if (dockerHost is not null)
                return dockerHost;

            return GetLocalMachineIp();
        }

        private async Task<IPAddress?> ResolveDockerHostAsync()
        {
            try
            {
                var entry = await Dns.GetHostEntryAsync("host.docker.internal");
                return entry.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            }
            catch
            {
                return null;
            }
        }

        private IPAddress? GetLocalMachineIp()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                var props = ni.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                    {
                        return addr.Address;
                    }
                }
            }

            return null;
        }
    }
}
