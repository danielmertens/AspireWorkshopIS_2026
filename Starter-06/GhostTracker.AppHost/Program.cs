var builder = DistributedApplication.CreateBuilder(args);

var ghostManagerApi = builder.AddProject<Projects.GhostTracker_GhostManager>("ghostmanagerapi");
var pathfinderApi = builder.AddProject<Projects.GhostTracker_PathFinderApi>("pathfinderapi");

builder.AddProject<Projects.GhostTracker_Bff>("bff")
    .WithReference(ghostManagerApi)
    .WithReference(pathfinderApi);

builder.AddViteApp("react", "../GhostTracker.React")
    .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
    .WithHttpEndpoint(env: "PORT", name: "FrontendEndpoint"); // We will be forwarding a random port on which the frontend will run.

for (int i = 1; i < 6; i++)
{
    builder.AddProject<Projects.GhostTracker_Transmitter>($"ghosttracker-transmitter-{i}")
        .WithHttpEndpoint()
        .WithReference(ghostManagerApi)
        .WithReference(pathfinderApi)
        .WithEnvironment("GhostId", i.ToString());
}

builder.Build().Run();
