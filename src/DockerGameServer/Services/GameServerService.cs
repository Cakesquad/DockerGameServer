using DockerGameServer.Models;
using DockerGameServer.Models.Database;
using DockerGameServer.Models.Enums;
using DockerGameServer.Repositories;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DockerGameServer.Services
{
	public class CreateGameServerModel<T>
	{
		[Required(ErrorMessage = "Server name is required.")]
		public string ServerName { get; set; }

		[Required(ErrorMessage = "Server kind is required.")]
		public GameServerKind ServerKind { get; set; }

		public T ServerConfiguration { get; set; }
	}

	public class GameServerService(GameServerRepository gameServerRepository, FileService fileService, DockerService dockerService, UserContext userContext)
	{
		public async Task<string> CreateAsync<T>(CreateGameServerModel<T> model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var gameServer = new GameServer
			{
				OwnerId = userContext.GetSignedInUser()?.Id ?? Guid.Empty,
				ServerName = model.ServerName,
				ServerKind = model.ServerKind,
				ServerConfiguration = JsonSerializer.Serialize(model.ServerConfiguration)
			};

			var serverDirectory = fileService.CreateServerDirectory(gameServer.Id.ToString());

			switch (model.ServerKind)
			{
				case GameServerKind.Minecraft:
					var minecraftConfig = JsonSerializer.Deserialize<MinecraftServer>(gameServer.ServerConfiguration);
					await CreateMinecraftServer(minecraftConfig!, gameServer.Id.ToString());
					break;
				default:
					throw new NotSupportedException($"Game server kind '{model.ServerKind}' is not supported.");
            }

            await gameServerRepository.AddAsync(gameServer);

            return gameServer.Id.ToString();
		}

		public async Task<bool> RemoveAsync(Guid serverId)
		{
			if (serverId == Guid.Empty)
				throw new ArgumentException("Id cannot be empty.", nameof(serverId));

			var gameServer = await gameServerRepository.GetByIdAsync(serverId);
			if (gameServer == null)
				throw new InvalidOperationException("Game server not found.");
			
			var containerId = await dockerService.GetContainerIdByNameAsync($"gameserver-{serverId}");
			if (!await dockerService.StopContainerAsync(containerId))
				throw new InvalidOperationException("Failed to stop game server container.");
			if (!await dockerService.RemoveContainerAsync(containerId, true))
				throw new InvalidOperationException("Failed to remove game server container.");

			fileService.DeleteServerDirectory(serverId.ToString());

			await gameServerRepository.DeleteAsync(gameServer);
			return true;
		}

		public async Task<List<GameServer>> GetAllByOwnerAsync(Guid ownerId)
		{
			if (ownerId == Guid.Empty)
				throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));

			return await gameServerRepository.GetByOwnerIdAsync(ownerId);
		}

		#region Help functions

		public async Task<DockerCreateResult> CreateMinecraftServer(MinecraftServer server, string serverId)
		{
			var env = new Dictionary<string, string>
			{
				{ "EULA", "TRUE" },
				{ "MEMORY", $"{server.Memory}G" },
			};

			switch (server.ServerType)
			{
				case MinecraftServerType.Vanilla:
					break;
				//case MinecraftServerType.Bedrock:
				//	env["TYPE"] = "BEDROCK";
				//	break;
				case MinecraftServerType.Fabric:
					env["TYPE"] = "FABRIC";
					env["MODRINTH_PROJECTS"] = "fabric-api";
					break;
				case MinecraftServerType.NeoForge:
					env["TYPE"] = "NEOFORGE";
					break;
				default:
					throw new NotSupportedException($"Minecraft server type '{server.ServerType}' is not supported.");
			}

			var result = await dockerService.CreateAndStartContainerAsync(
				image: "itzg/minecraft-server",
				name: $"gameserver-{serverId}",
				env: env,
				ports: server.Ports,
				volumeBinds: new List<string> { $"{fileService.GetServerDirectory(serverId)}:/data" }
			);

			return result;
		}

		#endregion
	}
}