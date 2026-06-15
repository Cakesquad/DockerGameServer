using DockerGameServer.Components;
using DockerGameServer.Data;
using DockerGameServer.Data.Interceptors;
using DockerGameServer.Repositories;
using DockerGameServer.Services;
using DockerGameServer.Services.Games;
using Microsoft.AspNetCore.Authentication.Cookies;
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
            builder.Services.AddControllers();

            builder.Services.AddScoped<EncryptionService>();
            builder.Services.AddScoped<EncryptionInterceptor>();
            builder.Services.AddScoped<TimestampInterceptor>();
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ServerPortRepository>();
            builder.Services.AddScoped<GameServerRepository>();
            builder.Services.AddScoped<GameServerService>();
            builder.Services.AddScoped<UserContext>();

            builder.Services.AddSingleton<DockerService>();
            builder.Services.AddSingleton<FileService>();

            builder.Services.AddHttpClient<MinecraftVersionService>();

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

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.LoginPath = "/login";
                    options.LogoutPath = "/auth/logout";
                });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();

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
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            app.MapControllers();

            app.Run();
        }
    }
}