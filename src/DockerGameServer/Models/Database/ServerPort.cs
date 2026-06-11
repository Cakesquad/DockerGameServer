namespace DockerGameServer.Models.Database
{
	public class ServerPort : BaseEntity
	{
		public Guid GameServerId { get; set; }
		public int InternalPort { get; set; }
		public int ExternalPort { get; set; }

		// Relations
		public GameServer GameServer { get; set; }
	}
}