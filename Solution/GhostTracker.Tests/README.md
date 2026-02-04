# GhostTracker Integration Tests

This project contains integration tests for the GhostTracker distributed application using .NET Aspire's testing framework.

## Overview

These tests verify the behavior of the entire distributed application, including:
- **GhostTracker.Transmitter** - Background service that transmits ghost locations
- **GhostTracker.GhostManager** - API that tracks ghost online/offline status
- **RabbitMQ** - Message broker (when applicable)

Unlike unit tests, these are **closed-box integration tests** that:
- Start the complete AppHost and all its resources
- Run services as separate processes (not in-memory)
- Test real service-to-service communication
- Verify end-to-end scenarios

## Test Structure

### `TransmitterTests` Class

Uses XUnit's `IAsyncLifetime` interface to efficiently manage the AppHost:
- **`InitializeAsync()`** - Creates and starts the AppHost once for all tests
- **`DisposeAsync()`** - Cleans up resources after all tests complete

This approach is more efficient than recreating the AppHost for each test.

### Key Tests

#### ✅ `Transmitter_UpdatesGhostStatus_WhenTakenOffline()`

**Purpose:** Verifies the transmitter offline control flow  
**Scenario:** 
1. Wait for transmitter and ghost manager services to be healthy
2. Verify ghost #1 is online (transmitters auto-start online)
3. Call `POST /offline` on the transmitter
4. Verify ghost #1 status changes to offline in GhostManager API

**What it validates:**
- Transmitter HTTP endpoint responds correctly
- Worker's `StopWorkerAsync()` properly pauses transmission
- GhostManager API receives and processes offline status
- Distributed application communication works end-to-end

#### ✅ `GhostManagerApi_ReturnsGhostData_WhenQueried()`

**Purpose:** Smoke test to verify basic API functionality  
**Scenario:**
1. Wait for GhostManager API to be healthy
2. Query ghost #1 data
3. Verify correct ghost information is returned

## Running the Tests

### From Command Line

```bash
# Navigate to the solution directory
cd Z:\AspireWorkshop\Solution

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run only GhostTracker.Tests
dotnet test GhostTracker.Tests

# Run a specific test
dotnet test --filter "FullyQualifiedName~Transmitter_UpdatesGhostStatus_WhenTakenOffline"
```

### From Visual Studio / VS Code

1. Open Test Explorer
2. Click "Run All Tests" or run individual tests
3. View test output in the Test Explorer window

## Test Configuration

### Timeouts

- **Default timeout:** 45 seconds per operation
- This allows time for services to start, especially on slower machines
- RabbitMQ container may take 10-20 seconds to become healthy

### Logging

Tests are configured with filtered logging to reduce noise:
- **Information level** for application logs
- **Warning level** for ASP.NET Core infrastructure
- **Warning level** for Aspire hosting (DCP) logs

To enable more verbose logging, modify `InitializeAsync()`:

```csharp
logging.SetMinimumLevel(LogLevel.Debug);
logging.AddFilter("Aspire.", LogLevel.Debug);
```

### Resource Health Checks

Tests use `WaitForResourceHealthyAsync()` to ensure services are ready:

```csharp
await app.ResourceNotifications.WaitForResourceHealthyAsync(
    "GhostTracker-transmitter-1", cancellationToken);
```

This prevents flaky tests due to services not being ready.

## Aspire Testing Features Used

### DistributedApplicationTestingBuilder

Creates a test host for the entire distributed application:

```csharp
var appHost = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.GhostTracker_AppHost>();
```

### CreateHttpClient

Creates an HttpClient configured for a specific resource:

```csharp
var transmitterClient = app.CreateHttpClient("GhostTracker-transmitter-1");
```

Aspire automatically:
- Resolves the correct endpoint (with service discovery)
- Configures base address
- Applies resilience policies (when configured)

### Resource Notifications

Monitors resource state changes:

```csharp
await app.ResourceNotifications.WaitForResourceHealthyAsync(
    "ghostmanagerapi", cancellationToken);
```

Possible states: `Starting`, `Running`, `Healthy`, `Unhealthy`, `Finished`

## Troubleshooting

### Tests Timeout

**Problem:** Tests fail with timeout errors  
**Solutions:**
- Increase `DefaultTimeout` value
- Check that Docker is running (RabbitMQ requires containers)
- Verify no port conflicts (check if services are already running)

### Services Not Starting

**Problem:** Resources fail to start or become healthy  
**Solutions:**
- Check Docker Desktop is running
- Review logs in test output for specific errors
- Verify RabbitMQ container can start successfully
- Run `dotnet run` in AppHost project to verify manually

### Ghost Still Online After Offline Call

**Problem:** Test assertion fails - ghost remains online  
**Solutions:**
- Check transmitter logs to verify `/offline` endpoint was called
- Verify GhostManager API received the offline request
- Add longer delay between offline call and status check
- Check network connectivity between services

### Cannot Resolve Resource Name

**Problem:** `CreateHttpClient("resource-name")` fails  
**Solutions:**
- Verify resource name matches exactly what's in AppHost `Program.cs`
- Resource names are case-sensitive
- Check AppHost successfully started all resources

## Best Practices

### ✅ DO
- Use `IAsyncLifetime` to share AppHost across tests
- Wait for resources to be healthy before testing
- Use appropriate timeouts with CancellationToken
- Test end-to-end scenarios, not internal implementation
- Include smoke tests to verify basic functionality

### ❌ DON'T
- Don't mock or replace services (tests run in separate processes)
- Don't access internal service state directly
- Don't assume instant startup - always wait for health checks
- Don't share mutable state between tests
- Don't test implementation details - focus on observable behavior

## References

- [Aspire Testing Documentation](https://aspire.dev/testing/overview/)
- [Write Your First Test](https://aspire.dev/testing/write-your-first-test/?testing-framework=xunit)
- [Manage AppHost in Tests](https://aspire.dev/testing/manage-app-host/?testing-framework=xunit)
- [Access Resources in Tests](https://aspire.dev/testing/accessing-resources/)

## Architecture

```
┌─────────────────────┐
│  TransmitterTests   │  (This test project)
└──────────┬──────────┘
           │ starts
           ↓
┌─────────────────────┐
│   AppHost Process   │
└──────────┬──────────┘
           │ orchestrates
           ↓
    ┌──────────────────────────────────────┐
    │                                      │
    ↓                                      ↓
┌─────────────────┐              ┌──────────────────┐
│  Transmitter-1  │──── HTTP ───→│  GhostManager    │
│  (Port 9001)    │              │  API             │
└─────────────────┘              └──────────────────┘
         │
         │ uses
         ↓
    ┌─────────────────┐
    │   RabbitMQ      │
    │   Container     │
    └─────────────────┘

Test sends HTTP → Transmitter → Updates → GhostManager API
Test verifies      ↓ /offline              ↓ Ghost.Online = false
```

The test project interacts with services via HTTP, validating real distributed application behavior.
