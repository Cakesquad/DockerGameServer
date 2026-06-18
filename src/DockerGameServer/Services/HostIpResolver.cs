using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DockerGameServer.Services
{
    public class HostIpResolver
    {
        private readonly HttpClient _http;

        public HostIpResolver(HttpClient http)
        {
            _http = http;
        }

        public async Task<IPAddress?> GetPublicIpAsync()
        {
            try
            {
                var ipString = await _http.GetStringAsync("https://api.ipify.org");
                if (IPAddress.TryParse(ipString, out var ip))
                    return ip;

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IPAddress?> GetHostIpAsync()
        {
            try
            {
                var entry = await Dns.GetHostEntryAsync("host.docker.internal");
                var dockerIp = entry.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                if (dockerIp != null)
                    return dockerIp;
            }
            catch { }

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
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

        public async Task<IPAddress?> GetServerIpAsync(bool useHostIp = false)
        {
            return useHostIp
                ? await GetHostIpAsync()
                : await GetPublicIpAsync();
        }
    }
}
