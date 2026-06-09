using DockerGameServer.Data;
using Microsoft.EntityFrameworkCore;

namespace DockerGameServer.Services
{
	public class MigrationService(IServiceProvider services) : IHostedService
	{
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			const int MaxRetries = 10;
			var delay = TimeSpan.FromSeconds(5);

			for (int i = 0; i < MaxRetries; i++)
			{
				try
				{
					using var scope = services.CreateScope();
					var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
					await dbContext.Database.MigrateAsync(cancellationToken);
					return;
				}
				catch (Exception)
				{
					if (i == MaxRetries - 1)
						throw;

					await Task.Delay(delay, cancellationToken);
				}
			}
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}