# Step 8 - Testing Your Aspire Application

Testing distributed applications can be challenging - you need to ensure all services are running, properly configured, and communicating correctly. .NET Aspire provides powerful testing capabilities that make it easier to write integration tests for your distributed applications.

In this exercise, we'll create integration tests for the GhostTracker application to verify that:
- Services start correctly and become healthy
- The transmitter can be taken offline via its API
- The GhostManager API correctly reflects the transmitter's status

## Understanding Aspire Testing

Aspire testing is built around **integration tests** rather than unit tests. These tests:
- Spin up the entire AppHost (or portions of it)
- Start real containers and services
- Verify end-to-end behavior across service boundaries
- Use the same orchestration logic as your production environment

The key component is the `DistributedApplicationTestingBuilder`, which creates a test harness for your AppHost project.

## Create the Test Project

First, let's create a new test project specifically for Aspire integration tests.

Open a terminal in the `Solution` folder and run:

```powershell
dotnet new aspire-xunit -o GhostTracker.Tests
```

This creates a new test project using the Aspire xUnit template, which includes:
- The `Aspire.Hosting.Testing` NuGet package
- xUnit test framework packages
- A sample test file (IntegrationTest1.cs)

### Add the Test Project to the Solution

Add the test project to your solution:

```powershell
dotnet sln add GhostTracker.Tests/GhostTracker.Tests.csproj
```

### Add Reference to AppHost

The test project needs a reference to the AppHost to orchestrate the distributed application:

```powershell
dotnet add GhostTracker.Tests/GhostTracker.Tests.csproj reference GhostTracker.AppHost/GhostTracker.AppHost.csproj
```

## Explore the Test Project Structure

Open the `GhostTracker.Tests.csproj` file to see its structure:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.Testing" Version="9.1.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\GhostTracker.AppHost\GhostTracker.AppHost.csproj" />
  </ItemGroup>
</Project>
```

Key components:
- **`Aspire.Hosting.Testing`**: Core package for Aspire testing
- **xUnit packages**: Test framework and runner
- **AppHost reference**: Allows tests to orchestrate the distributed application

## Understanding Test Lifecycle with IAsyncLifetime

For integration tests, starting the entire distributed application for each test can be slow and resource-intensive. Instead, we can start the AppHost once and reuse it across multiple tests.

xUnit provides the `IAsyncLifetime` interface for this purpose:

```csharp
public interface IAsyncLifetime
{
    Task InitializeAsync();  // Called once before all tests
    Task DisposeAsync();     // Called once after all tests
}
```

This pattern significantly improves test performance by:
- Starting the AppHost once for the entire test class
- Reusing containers and services across tests
- Properly cleaning up resources after all tests complete

## Create Your First Integration Test

Now let's create a comprehensive integration test. Replace the contents of `IntegrationTest1.cs` with:

```csharp
using System.Net.Http.Json;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostTracker.Tests;

/// <summary>
/// Integration tests for the GhostTracker Transmitter service.
/// These tests verify the distributed application behavior using the Aspire AppHost.
/// </summary>
public class TransmitterTests : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(45);
    private DistributedApplication? _app;

    /// <summary>
    /// Initialize the AppHost once for all tests in this class.
    /// This improves test performance by reusing the distributed application instance.
    /// </summary>
    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.GhostTracker_AppHost>();

        // Add resilience to HTTP client calls
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        _app = await appHost.BuildAsync();
        await _app.StartAsync();
    }

    /// <summary>
    /// Clean up the AppHost after all tests complete.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// Test: Verify that when a transmitter is taken offline, 
    /// it stops transmitting location data and the ghost status is updated.
    /// 
    /// This test validates the core offline control flow:
    /// 1. Transmitter starts online and actively transmitting
    /// 2. POST /offline endpoint is called
    /// 3. Ghost status in GhostManager API changes to offline
    /// </summary>
    [Fact]
    public async Task Transmitter_UpdatesGhostStatus_WhenTakenOffline()
    {
        // Arrange
        using var cts = new CancellationTokenSource(DefaultTimeout);
        
        // Wait for critical services to be healthy
        await _app!.ResourceNotifications.WaitForResourceHealthyAsync("GhostTracker-transmitter-1", cts.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("ghostmanagerapi", cts.Token);

        // Create HTTP clients for the transmitter and ghost manager API
        var transmitterClient = _app.CreateHttpClient("GhostTracker-transmitter-1");
        var ghostManagerClient = _app.CreateHttpClient("ghostmanagerapi");

        // Wait a moment for the transmitter to start and come online
        await Task.Delay(2000, cts.Token);

        // Verify ghost is online initially (transmitter comes online at startup)
        // Allow for some retries as the initial status update may take time
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var initialGhostCheck = await ghostManagerClient.GetFromJsonAsync<Ghost>("/ghosts/1", cts.Token);
            if (initialGhostCheck is not null && initialGhostCheck.Online)
            {
                break;
            }
            await Task.Delay(1000, cts.Token);
        }

        // Act - Take the transmitter offline
        var offlineResponse = await transmitterClient.PostAsync("/offline", null, cts.Token);
        Assert.True(offlineResponse.IsSuccessStatusCode, "Offline request should succeed");

        // Give the system a moment to process the status change
        await Task.Delay(500, cts.Token);

        // Assert - Verify ghost status is now offline
        var offlineGhost = await ghostManagerClient.GetFromJsonAsync<Ghost>("/ghosts/1", cts.Token);
        Assert.NotNull(offlineGhost);
        Assert.False(offlineGhost.Online, "Ghost should be offline after transmitter is taken offline");
    }

}

/// <summary>
/// Ghost model matching the GhostManager API response.
/// </summary>
public record Ghost
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Age { get; set; }
    public DateTime DateOfDead { get; set; }
    public string HauntLocation { get; set; } = string.Empty;
    public string Appearance { get; set; } = string.Empty;
    public int DangerLevel { get; set; }
    public string Abilities { get; set; } = string.Empty;
    public bool Online { get; set; }
}
```

## Understanding the Test Code

Let's break down the key components:

### 1. Test Class Setup with IAsyncLifetime

```csharp
public class TransmitterTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    
    public async Task InitializeAsync() { /* ... */ }
    public async Task DisposeAsync() { /* ... */ }
}
```

The `IAsyncLifetime` interface ensures:
- The AppHost is created **once** before any tests run
- All tests share the same application instance
- Resources are properly cleaned up after tests complete

### 2. Creating the Test AppHost

```csharp
var appHost = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.GhostTracker_AppHost>();
```

The `DistributedApplicationTestingBuilder.CreateAsync<T>()` method:
- Takes your AppHost project type as a generic parameter
- Creates a test harness that wraps your AppHost
- Provides access to services, configuration, and resources

### 3. Adding HTTP Resilience

```csharp
appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
{
    clientBuilder.AddStandardResilienceHandler();
});
```

The standard resilience handler adds:
- Automatic retries for transient failures
- Timeout handling
- Circuit breaker patterns
- This makes tests more reliable when services are starting up

### 4. Waiting for Resources to Be Ready

```csharp
await _app.ResourceNotifications.WaitForResourceHealthyAsync(
    "ghostmanagerapi", cts.Token);
```

**Critical for reliable tests:**
- Services start asynchronously in containers
- Just because a container is running doesn't mean it's ready to serve requests
- `WaitForResourceHealthyAsync` waits for health checks to pass
- Always provide a timeout via CancellationToken to prevent hanging

### 5. Creating HTTP Clients for Resources

```csharp
var ghostManagerClient = _app.CreateHttpClient("ghostmanagerapi");
```

The `CreateHttpClient` method:
- Takes the **resource name** from your AppHost
- Returns an `HttpClient` configured to connect to that resource
- Automatically handles endpoint resolution
- Applies any configured resilience policies

### 6. Using Arrange-Act-Assert Pattern

```csharp
// Arrange - Set up test conditions
await _app.ResourceNotifications.WaitForResourceHealthyAsync(...);
var client = _app.CreateHttpClient(...);

// Act - Perform the action being tested
var response = await client.PostAsync("/offline", null);

// Assert - Verify the expected outcome
Assert.True(response.IsSuccessStatusCode);
```

This standard testing pattern makes tests easy to read and maintain.

## Improving Test Diagnostics with Logging

By default, you may see a lot of noise in the test output from infrastructure components. You can configure logging to filter out unnecessary messages and focus on your application logs.

Add the following logging configuration in your `InitializeAsync()` method, after creating the `appHost`:

```csharp
public async Task InitializeAsync()
{
    var appHost = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.GhostTracker_AppHost>();

    // Configure logging for better test diagnostics
    appHost.Services.AddLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
        logging.AddFilter("Default", LogLevel.Information);
        logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Warning);
        // Suppress RabbitMQ container noise (connection warnings, etc.)
        logging.AddFilter("GhostTracker.AppHost.Resources.messaging", LogLevel.Error);
    });

    // Add resilience to HTTP client calls
    appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
    {
        clientBuilder.AddStandardResilienceHandler();
    });

    _app = await appHost.BuildAsync();
    await _app.StartAsync();
}
```

**Why configure logging in tests?**

Since test projects don't automatically inherit your application's configuration:
- You must explicitly configure logging if you want to control it
- Filter out noisy infrastructure logs (ASP.NET Core, DCP, RabbitMQ)
- Keep application-specific logs at Information level
- This prevents test output from being overwhelmed with infrastructure messages

## Best Practices for Aspire Testing

1. **Use IAsyncLifetime**: Share the AppHost across tests to improve performance
2. **Always wait for healthy**: Use `WaitForResourceHealthyAsync` before making requests
3. **Use timeouts**: Always provide a `CancellationToken` with a timeout to prevent hanging tests
4. **Configure logging**: Filter out infrastructure noise to focus on application logs
5. **Add resilience**: Use `AddStandardResilienceHandler` to handle transient failures
6. **Test end-to-end**: Focus on integration scenarios that cross service boundaries
7. **Keep tests focused**: Each test should verify one specific behavior
8. **Use descriptive names**: Test names should clearly describe what they verify
9. **Clean up resources**: Implement `DisposeAsync` properly to clean up the AppHost

## Additional Resources

- [Aspire Testing Documentation](https://aspire.dev/testing/overview/)
- [Write Your First Test](https://aspire.dev/testing/write-your-first-test/?testing-framework=xunit)
- [Manage AppHost in Tests](https://aspire.dev/testing/manage-app-host/?testing-framework=xunit)
- [Access Resources in Tests](https://aspire.dev/testing/accessing-resources/)
- [xUnit Documentation](https://xunit.net/)

## Extra Challenges (Optional)

Try extending your test coverage:

1. **Add a GhostManager API smoke test**: Create a test that verifies the GhostManager API is accessible and returns ghost data. This is a basic smoke test to ensure the distributed application is running correctly.
2. **Test the PathFinder API**: Create a test that verifies the pathfinding service returns valid routes
3. **Test Resource Dependencies**: Verify that services fail gracefully when dependencies are unavailable
4. **Test Environment Variables**: Verify that environment variables are correctly injected into resources
5. **Test with Different Configurations**: Use `DistributedApplicationFactory` to test different configuration scenarios
6. **Add Coverage Reporting**: Configure code coverage tools to measure test coverage
7. **Test RabbitMQ Integration**: Verify messages are being published and consumed correctly

## Summary

In this exercise, you learned how to:
- ✅ Create an Aspire test project using the `aspire-xunit` template
- ✅ Use `DistributedApplicationTestingBuilder` to create a test harness
- ✅ Implement `IAsyncLifetime` to share the AppHost across tests
- ✅ Configure logging and resilience for test reliability
- ✅ Wait for resources to be healthy before testing
- ✅ Create HTTP clients for testing service endpoints
- ✅ Write integration tests that verify end-to-end behavior
- ✅ Follow best practices for distributed application testing

With these testing capabilities, you can ensure your distributed application works correctly across service boundaries and maintains reliability through continuous integration and deployment.

---

[Next Exercise →](./exercise_09.md)
