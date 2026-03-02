var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.GhostTracker_Bff>("bff");
builder.AddProject<Projects.GhostTracker_GhostManager>("ghostmanagerapi");
builder.AddProject<Projects.GhostTracker_PathFinderApi>("pathfinderapi");

builder.AddViteApp("react", "../GhostTracker.React")
    .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
    .WithHttpEndpoint(env: "PORT", name: "FrontendEndpoint"); // We will be forwarding a random port on which the frontend will run.

builder.Build().Run();
