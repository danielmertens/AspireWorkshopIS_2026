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

    /// <summary>
    /// Test: Verify that the GhostManager API is accessible and returns ghost data.
    /// This is a basic smoke test to ensure the distributed application is running correctly.
    /// </summary>
    [Fact]
    public async Task GhostManagerApi_ReturnsGhostData_WhenQueried()
    {
        // Arrange
        using var cts = new CancellationTokenSource(DefaultTimeout);
        
        await _app!.ResourceNotifications.WaitForResourceHealthyAsync(
            "ghostmanagerapi", cts.Token);

        var ghostManagerClient = _app.CreateHttpClient("ghostmanagerapi");

        // Act
        var ghost = await ghostManagerClient.GetFromJsonAsync<Ghost>("/ghosts/1", cts.Token);

        // Assert
        Assert.NotNull(ghost);
        Assert.Equal(1, ghost.Id);
        Assert.Equal("Marshmallow Man", ghost.Name);
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
