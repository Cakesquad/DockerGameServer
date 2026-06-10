namespace DockerGameServer.Models
{
	public class MinecraftServer
	{
		public int Memory { get; set; } = 4;
		public MinecraftServerType ServerType { get; set; } = MinecraftServerType.Java;
		public Dictionary<int, int> Ports { get; set; } = new Dictionary<int, int>();
		public string? Version { get; set; }
		public string JavaVersion { get; set; } = "25";
	}

	public enum MinecraftServerType
	{
		Java,
		Bedrock,
		Fabric,
		NeoForge
	}
}