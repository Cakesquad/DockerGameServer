using DockerGameServer.Data;
using DockerGameServer.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace DockerGameServer.Repositories
{
	public class UserRepository(AppDbContext dbContext)
	{
		public async Task<bool> IsEmailRegisteredAsync(string emailHash)
		{
			return await dbContext.Users.AnyAsync(u => u.EmailHash == emailHash);
		}

		public async Task<User?> GetByIdAsync(Guid id)
		{
			return await dbContext.Users.FindAsync(id);
		}

		public async Task<User?> GetByEmailHashAsync(string emailhash)
		{
			return await dbContext.Users.FirstOrDefaultAsync(u => u.EmailHash == emailhash);
		}

		public async Task AddAsync(User user)
		{
			await dbContext.Users.AddAsync(user);
			await dbContext.SaveChangesAsync();
		}

		public async Task UpdateAsync(User user)
		{
			dbContext.Users.Update(user);
			await dbContext.SaveChangesAsync();
		}

		public async Task DeleteAsync(User user)
		{
			dbContext.Users.Remove(user);
			await dbContext.SaveChangesAsync();
		}
	}
}