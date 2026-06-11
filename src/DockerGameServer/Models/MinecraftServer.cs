namespace DockerGameServer.Models
{
	public class MinecraftServer
	{
		public int Memory { get; set; } = 4;
        public MinecraftServerType ServerType { get; set; } = MinecraftServerType.Vanilla;
		public Dictionary<int, int> Ports { get; set; } = new Dictionary<int, int>();
        public string? Version { get; set; }
        public JavaVersion JavaVersion { get; set; } = JavaVersion.java25;
	}

	public enum MinecraftServerType
	{
		Vanilla,
		//Bedrock,
		Fabric,
		NeoForge
	}

	public enum JavaVersion
    {
        java25 = 25,
		java21 = 21,
        java17 = 17,
		java16 = 16,
        java11 = 11,
        java8 = 8
    }
}