# Step 5 - Time to find some ghosts

Now to start adding our transmitter into the mix we need to finish the configuration of our transmitter. Open the `program.cs` of the `GhostTracker.Transmitter` project.

Start by adding a reference to the ServiceDefaults project and include the service defaults.

```c#
builder.AddServiceDefaults();
...
app.MapDefaultEndpoints();
```

Next we can add the HttpClients to the service using Service Discovery.

```c#
builder.Services.AddHttpClient<GhostManagerApiClient>(static client => client.BaseAddress = new("https+http://ghostmanagerapi"));
builder.Services.AddHttpClient<PathFinderApiClient>(static client => client.BaseAddress = new("https+http://pathfinderapi"));
```

> **Note:** The `https+http://` scheme tells Service Discovery to try HTTPS first, then fall back to HTTP if needed. This is useful for services that might support both protocols during development.

The last thing we need to setup in this service is specifying the GhostId. Any value from 1 to 10 would work here since those are the id's known by our manager. We would however like to have some control over which ghost our transmitter is tracking from our AppHost. Therefore we are going to read the `GhostId` from our environment variables as follows:

```c#
builder.Services.AddSingleton((provider) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new GhostContext
    {
        GhostId = configuration.GetValue<int>("GhostId")
    };
});
```

> **Note:** The `GhostContext` class is already defined in the Transmitter project's Models folder. It's a simple container for passing the Ghost ID through dependency injection.

To add a single transmitter to the project we can add it as a new project in our AppHost and make the correct references like this:

```c#
builder.AddProject<Projects.GhostTracker_Transmitter>("ghosttracker-transmitter")
    .WithHttpEndpoint()
    .WithReference(ghostManagerApi)
    .WithReference(pathfinderApi)
    .WithEnvironment("GhostId", "1");
```

This adds the transmitter with a reference to the manager and pathfinder and sets the `GhostId` environment variable to 1.

For development it is however handy if we can test with multiple transmitters active. In a traditional project this would be a problem, starting the same project multiple times in debug mode is hard, if not impossible. With Aspire however, this becomes a piece of cake.

We can simply create a loop in our AppHost and create multiple transmitters:

```c#
for (int i = 1; i < 6; i++)
{
    builder.AddProject<Projects.GhostTracker_Transmitter>($"ghosttracker-transmitter-{i}")
        .WithReference(ghostManagerApi)
        .WithReference(pathfinderApi)
        .WithEnvironment("GhostId", i.ToString());
}
```

This creates 5 transmitters (Ghost IDs 1-5). We're using 5 transmitters for demonstration purposes - enough to make the application interesting without overwhelming your development machine. The system supports up to 10 ghosts total.

> **Important:** All resources in Aspire need a unique identifier. When creating multiple instances of the same service, make sure to include a unique suffix in the resource name.

## Verifying Your Transmitters

After running the application, you can verify everything is working:

1. **Check the Aspire Dashboard** - you should see 5 transmitter services listed (ghosttracker-transmitter-1 through ghosttracker-transmitter-5)
2. **Review the logs** - each transmitter should be sending location updates for their assigned ghost
3. **Open the React frontend** - you should see 5 ghosts moving on the map

This demonstrates one of Aspire's key advantages: easily running multiple instances of the same service during development, something that would be complex to set up with traditional approaches.

## Additional Resources

- [External Parameters](https://aspire.dev/fundamentals/external-parameters)
- [Resource model](https://aspire.dev/architecture/resource-model)

---

[Next Exercise →](./exercise_06.md)
