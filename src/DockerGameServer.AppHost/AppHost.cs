var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("dockergameserver-postgres")
	.WithDataVolume()
	.WithPgWeb();

var database = postgres.AddDatabase("PostgresDb");

builder.AddProject<Projects.DockerGameServer>("dockergameserver")
	.WithReference(database)
	.WaitFor(postgres)
	.WaitFor(database);

builder.Build().Run();
