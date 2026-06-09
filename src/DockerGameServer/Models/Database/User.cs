namespace DockerGameServer.Models.Database
{
	public class UserInfo
	{
		public string FullName { get; set; }
		public string Email { get; set; }
	}

	public class User : EncryptedDataEntity<UserInfo>
	{
		public required string PasswordHash { get; set; }
		public required string EmailHash { get; set; }
	}
}