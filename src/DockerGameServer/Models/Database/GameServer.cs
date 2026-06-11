using DockerGameServer.Models.Enums;

namespace DockerGameServer.Models.Database
{
	public class GameServer : BaseEntity
	{
		public Guid OwnerId { get; set; }
		public string ServerName { get; set; }
		public GameServerKind ServerKind { get; set; }
		public string ServerConfiguration { get; set; }


		// Relations
		public User Owner { get; set; }
		public List<ServerPort> ServerPorts { get; set; }
	}
}