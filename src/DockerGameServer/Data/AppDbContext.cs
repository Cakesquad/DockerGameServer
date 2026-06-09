using DockerGameServer.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace DockerGameServer.Data
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
	{
		public DbSet<User> Users { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			
			
		}
	}
}