using GhostTracker.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

var ghostManagerApi = builder.AddProject<Projects.GhostTracker_GhostManager>("ghostmanagerapi")
    .WithReference(rabbitmq);
var pathfinderApi = builder.AddProject<Projects.GhostTracker_PathFinderApi>("pathfinderapi")
    .WithReference(rabbitmq);

var bff = builder.AddProject<Projects.GhostTracker_Bff>("bff")
    .WithExternalHttpEndpoints()
    .WithReference(ghostManagerApi)
    .WithReference(pathfinderApi);

for (int i = 1; i < 4; i++)
{
    builder.AddProject<Projects.GhostTracker_Transmitter>($"GhostTracker-transmitter-{i}")
        .WithHttpEndpoint(port: 9000 + i)
        .WithEnvironment("GhostId", i.ToString())
        .WithReference(ghostManagerApi)
        .WithReference(pathfinderApi)
        .WaitFor(ghostManagerApi)
        .WaitFor(pathfinderApi)
        .AddTransmitterInteractions();
}

builder.AddViteApp("react", "../GhostTracker.React")
    .WithReference(bff)
    .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
    .WithHttpEndpoint(env: "PORT", name: "FrontendEndpoint")
    .PublishAsDockerFile();

builder.AddProject<Projects.GhostTracker_Transmitter_RabbitMQ>("ghosttracker-transmitter-rabbitmq")
    .WithEnvironment("GhostId", "9")
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

builder.Build().Run();
