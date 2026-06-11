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

		public List<Port> Ports { get; set; }
	}

	public class Port
	{
		public int InternalPort { get; set; }
		public int ExternalPort { get; set; }
	}

	public record ListGameServer
	(
		Guid Id,
		string ServerName,
		GameServerKind ServerKind,
		string ServerConfiguration,
		string State
	);

	public class GameServerService(GameServerRepository gameServerRepository, ServerPortRepository serverPortRepository, FileService fileService, DockerService dockerService)
	{
		public async Task<string> CreateAsync<T>(CreateGameServerModel<T> model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var gameServer = new GameServer
			{
				OwnerId = Guid.Parse("019eb0f6-d4a3-7074-b4ed-82e9bf9987e3"),
				ServerName = model.ServerName,
				ServerKind = model.ServerKind,
				ServerConfiguration = JsonSerializer.Serialize(model.ServerConfiguration)
			};
			await gameServerRepository.AddAsync(gameServer);

			foreach (var port in model.Ports)
			{
				var serverPort = new ServerPort
				{
					GameServerId = gameServer.Id,
					InternalPort = port.InternalPort,
					ExternalPort = port.ExternalPort
				};
				await serverPortRepository.AddAsync(serverPort);
			}

			var serverDirectory = fileService.CreateServerDirectory(gameServer.Id.ToString());

			switch (model.ServerKind)
			{
				case GameServerKind.Minecraft:
					var minecraftConfig = JsonSerializer.Deserialize<MinecraftServer>(gameServer.ServerConfiguration);
					await CreateMinecraftServer(minecraftConfig!, model.Ports, gameServer.Id.ToString());
					break;
				default:
					throw new NotSupportedException($"Game server kind '{model.ServerKind}' is not supported.");
			}

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

		public async Task<List<ListGameServer>> GetAllByOwnerAsync(Guid ownerId)
		{
			if (ownerId == Guid.Empty)
				throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));

			List<ListGameServer> result = new List<ListGameServer>();

			var gameServers = await gameServerRepository.GetByOwnerIdAsync(ownerId);
			if (gameServers == null)
				return result;

			var expectedNames = gameServers.Select(gs => $"gameserver-{gs.Id}")
				.ToList();

			var allServers = await dockerService.ListContainersAsync();
			if (allServers == null)
				return result;

			foreach (var server in allServers)
			{
				var dockerName = server.Name.TrimStart('/');

				if (!expectedNames.Contains(dockerName))
					continue;

				var matching = gameServers.FirstOrDefault(gs => $"gameserver-{gs.Id}" == dockerName);

				if (matching == null)
					continue;

				result.Add(new ListGameServer(
					Id: matching.Id,
					ServerName: matching.ServerName,
					ServerKind: matching.ServerKind,
					ServerConfiguration: matching.ServerConfiguration,
					State: server.State));
			}

			return result;
		}

		public async Task StopRunningServerAsync(Guid serverId)
		{
			if (serverId == Guid.Empty)
				throw new ArgumentNullException("Server ID cannot be empty.", nameof(serverId));

			var containerName = $"gameserver-{serverId}";
			var containerId = await dockerService.GetContainerIdByNameAsync(containerName);
			if (string.IsNullOrWhiteSpace(containerId))
				throw new InvalidOperationException("Game server container not found.");

			await dockerService.StopContainerAsync(containerId);
		}

		public async Task StartStoppedServerAsync(Guid serverId)
		{
			if (serverId == Guid.Empty)
				throw new ArgumentNullException("Server ID cannot be empty.", nameof(serverId));

			var containerName = $"gameserver-{serverId}";
			var containerId = await dockerService.GetContainerIdByNameAsync(containerName);
			if (string.IsNullOrWhiteSpace(containerId))
				throw new InvalidOperationException("Game server container not found.");

			await dockerService.StartContainerAsync(containerId);
		}

		#region Help functions

		public async Task<DockerCreateResult> CreateMinecraftServer(MinecraftServer server, List<Port> ports, string serverId)
		{
			var env = new Dictionary<string, string>
			{
				{ "EULA", "TRUE" },
				{ "MEMORY", $"{server.Memory}G" },
			};

			switch (server.ServerType)
			{
				case MinecraftServerType.Java:
					break;
				case MinecraftServerType.Bedrock:
					env["TYPE"] = "BEDROCK";
					break;
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
				ports: ports.ToDictionary(p => p.ExternalPort, p => p.InternalPort),
				volumeBinds: new List<string> { $"{fileService.GetServerDirectory(serverId)}:/data" }
			);

			return result;
		}

		#endregion
	}
}