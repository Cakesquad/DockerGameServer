namespace DockerGameServer.Models
{
	public class MinecraftServer
	{
		public int Memory { get; set; } = 4;
        public MinecraftServerType ServerType { get; set; } = MinecraftServerType.Vanilla;
        public string? Version { get; set; }
        public JavaVersion JavaVersion { get; set; } = JavaVersion.java25;
	}

	public enum MinecraftServerType
	{
		Vanilla,
		Fabric,
		NeoForge
	}

	public enum JavaVersion
    {
		latest,
        java25,
		java21,
        java17,
		java16,
        java11,
        java8
    }
}