namespace DockerGameServer.Services
{
    public class FileService(IConfiguration configuration)
    {
        private readonly string _serversPath = configuration["Servers:path"] ?? throw new InvalidOperationException("Servers:path is not configured.");

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
