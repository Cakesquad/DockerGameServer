namespace DockerGameServer.Services
{
    public class FileService(IConfiguration configuration)
    {
        private readonly string _serversPath = configuration["Servers:Path"] ?? throw new InvalidOperationException("Servers:Path is not configured.");
        private readonly string _serversSysPath = configuration["Servers:SysPath"] ?? throw new InvalidOperationException("Servers:SysPath is not configured.");

        public string CreateServerDirectory(string serverName)
        {
            var serverPath = Path.Combine(_serversPath, serverName);
            if (!Directory.Exists(serverPath))
            {
                Directory.CreateDirectory(serverPath);
            }
            return serverPath;
        }

        public string GetServerDirectory(string serverName)
        {
            return Path.Combine(_serversPath, serverName);
        }

        public string GetServerSysDirectory(string serverName)
        {
            return Path.Combine(_serversSysPath, serverName);
        }

        public void DeleteServerDirectory(string serverName)
        {
            var serverPath = Path.Combine(_serversPath, serverName);
            if (Directory.Exists(serverPath))
            {
                Directory.Delete(serverPath, true);
            }
        }
    }
}
