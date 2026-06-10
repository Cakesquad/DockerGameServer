using DockerGameServer.Data;
using DockerGameServer.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace DockerGameServer.Repositories
{
	public class GameServerRepository(AppDbContext dbContext)
	{
		public async Task<bool> DoesGameServerExistAsync(string serverName)
		{
			if (await dbContext.GameServers.FirstOrDefaultAsync(gs => gs.ServerName == serverName) == null)
			{
				return false;
			}
			return true;	
		}

		public async Task<List<GameServer>> GetAllAsync()
		{
			return await dbContext.GameServers.ToListAsync();
		}

		public async Task<List<GameServer>> GetByOwnerIdAsync(Guid ownerId)
		{
			return await dbContext.GameServers.Where(gs => gs.OwnerId == ownerId).ToListAsync();
		}

		public async Task<GameServer?> GetByIdAsync(Guid id)
		{
			return await dbContext.GameServers.FindAsync(id);
		}

		public async Task<GameServer?> GetByServerNameAsync(string serverName)
		{
			return await dbContext.GameServers.FirstOrDefaultAsync(gs => gs.ServerName == serverName);
		}


		public async Task AddAsync(GameServer gameServer)
		{
			dbContext.GameServers.Add(gameServer);
			await dbContext.SaveChangesAsync();
		}

		public async Task UpdateAsync(GameServer gameServer)
		{
			dbContext.GameServers.Update(gameServer);
			await dbContext.SaveChangesAsync();
		}

		public async Task DeleteAsync(GameServer gameServer)
		{
			dbContext.GameServers.Remove(gameServer);
			await dbContext.SaveChangesAsync();
		}
	}
}