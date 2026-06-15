using DockerGameServer.Data;
using DockerGameServer.Models.Database;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace DockerGameServer.Repositories
{
	public class ServerPortRepository(AppDbContext dbContext)
	{
		public async Task<bool> ExternalPortInUseAsync(int port)
		{
			return await dbContext.ServerPorts.AnyAsync(sp => sp.ExternalPort == port);
		}

		public async Task<List<ServerPort>> GetByServerIdAsync(Guid serverId)
		{
			return await dbContext.ServerPorts.Where(sp => sp.GameServerId == serverId).ToListAsync();
		}

		public async Task AddAsync(ServerPort serverPort)
		{
			dbContext.ServerPorts.Add(serverPort);
			await dbContext.SaveChangesAsync();
		}

		public async Task UpdateAsync(ServerPort serverPort)
		{
			dbContext.ServerPorts.Update(serverPort);
			await dbContext.SaveChangesAsync();
		}

		public async Task DeleteAsync(ServerPort serverPort)
		{
			dbContext.ServerPorts.Remove(serverPort);
			await dbContext.SaveChangesAsync();
		}
	}
}