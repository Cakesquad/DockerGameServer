var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DockerGameServer>("dockergameserver");

builder.Build().Run();
