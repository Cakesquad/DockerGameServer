using DockerGameServer.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace DockerGameServer.Data
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
	{
		public DbSet<User> Users { get; set; }
		public DbSet<GameServer> GameServers { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<GameServer>()
				.HasOne(g => g.Owner)
				.WithMany()
				.HasForeignKey(g => g.OwnerId);

			modelBuilder.Entity<GameServer>()
				.Property(g => g.ServerConfiguration)
				.HasColumnType("jsonb");
		}
	}
}