using DockerGameServer.Components;
using DockerGameServer.Data;
using DockerGameServer.Data.Interceptors;
using DockerGameServer.Repositories;
using DockerGameServer.Services;
using Microsoft.EntityFrameworkCore;

namespace DockerGameServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddScoped<EncryptionService>();
            builder.Services.AddScoped<EncryptionInterceptor>();
            builder.Services.AddScoped<TimestampInterceptor>();
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<GameServerRepository>();
			builder.Services.AddScoped<GameServerService>();

            builder.Services.AddSingleton<DockerService>();
            builder.Services.AddSingleton<FileService>();

            builder.Services.AddHostedService<MigrationService>();

            builder.Services.AddDbContext<AppDbContext>(
                         (serviceProvider, options) =>
                {
                    var connectionString = builder.Configuration.GetConnectionString("PostgresDb");
                    options.UseNpgsql(connectionString);
                    options.AddInterceptors(
                        serviceProvider.GetRequiredService<TimestampInterceptor>(),
                        serviceProvider.GetRequiredService<EncryptionInterceptor>());
                });

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}