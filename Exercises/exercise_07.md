# Step 7 - Interactive Commands with Aspire Interaction Service

The Aspire Interaction Service allows you to create interactive commands that can prompt users for input, display notifications, and request confirmations. These interactions work both in the Aspire dashboard (as modal dialogs and notifications) and in the CLI (as text prompts).

This is extremely useful for operational tasks like:
- Taking services online/offline
- Triggering administrative actions
- Gathering configuration input
- Confirming destructive operations

In this exercise, we'll add interactive commands to our transmitter resources that allow us to control their behavior directly from the dashboard.

## Understanding the Interaction Service

The `IInteractionService` interface provides several methods for interacting with users:

| Method | Description | Context |
|--------|-------------|---------|
| `PromptNotificationAsync` | Shows a non-modal notification message | Dashboard only |
| `PromptConfirmationAsync` | Shows a confirmation dialog | Dashboard only |
| `PromptMessageBoxAsync` | Shows a modal message box | Dashboard only |
| `PromptInputAsync` | Prompts for a single input value | Dashboard & CLI |
| `PromptInputsAsync` | Prompts for multiple input values | Dashboard & CLI |

For this exercise, we'll focus on notifications and confirmations which are perfect for operational commands in the dashboard.

## Creating the Interactions Extension

First, let's create a new file to house our transmitter interaction commands.

Create a new file in your AppHost project called `Interactions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace GhostTracker.AppHost;

#pragma warning disable ASPIREINTERACTION001
public static class TransmitterInteractions
{
    /// <summary>
    /// Adds interactive commands (Online, Offline, Teleport) to a transmitter resource.
    /// </summary>
    public static IResourceBuilder<ProjectResource> AddTransmitterInteractions(
        this IResourceBuilder<ProjectResource> transmitter)
    {
        // We'll add our commands here
        return transmitter;
    }

    private static async Task<ExecuteCommandResult> SuccessResult(IInteractionService interactionService, string message)
    {
        await interactionService.PromptNotificationAsync(
            title: "Success",
            message: message,
            options: new NotificationInteractionOptions
            {
                Intent = MessageIntent.Success
            });
        return CommandResults.Success();
    }

    private static async Task<ExecuteCommandResult> ErrorResult(IInteractionService interactionService, string message)
    {
        await interactionService.PromptNotificationAsync(
            title: "Error",
            message: message,
            options: new NotificationInteractionOptions
            {
                Intent = MessageIntent.Error
            });
        return CommandResults.Failure(message);
    }
}
```

The `#pragma warning disable ASPIREINTERACTION001` suppresses warnings about using the preview interaction service API.

### Helper Methods

Before we add the commands, notice the two helper methods at the bottom of the class:

- **`SuccessResult`**: Displays a success notification and returns a success result
- **`ErrorResult`**: Displays an error notification and returns a failure result

This uses the interaction service to display notifications at the top of the dashboard. Notice that you can set a title, message and provide some options. The `MessageIntent.Success` will display a green notification and the `MessageIntent.Error` will display a red notification.

We have extracted this logic to helper methods to improve readability of the other code we will be adding in this exercise.

## Adding the "Take Online" Command

Let's add our first interactive command - a simple button to bring a transmitter online.

Add the following code inside the `AddTransmitterInteractions` method (above the helper methods):

```csharp
// Add "Take Online" command
transmitter.WithCommand("online", "Take Online", async context =>
{
    var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();
    
    if (!interactionService.IsAvailable)
    {
        return CommandResults.Failure("Interaction service not available");
    }

    try
    {
        // Get HTTP endpoint for the transmitter
        var endpoint = transmitter.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.UriScheme == "http" || e.UriScheme == "https");

        if (endpoint == null)
        {
            return await ErrorResult(interactionService,  $"No HTTP endpoint found for {transmitter.Resource.Name}");
        }

        // Make HTTP call to /online endpoint
        var httpClient = new HttpClient();
        
        var port = endpoint.Port ?? (endpoint.UriScheme == "https" ? 443 : 80);
        var url = $"http://localhost:{port}/online";
        
        var response = await httpClient.PostAsync(url, null);
        
        if (response.IsSuccessStatusCode)
        {
            return await SuccessResult(interactionService, $"{transmitter.Resource.Name} is now online");
        }
        else
        {
            return await ErrorResult(interactionService, $"Failed to bring {transmitter.Resource.Name} online: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        return await ErrorResult(interactionService, $"Error bringing {transmitter.Resource.Name} online: {ex.Message}");
    }
});
```

### Understanding the Code

- **`WithCommand`**: Adds a command button to the resource in the dashboard
- **`IInteractionService`**: Retrieved from dependency injection to display notifications
- **`IsAvailable`**: Always check this before using the interaction service
- **Helper Methods**: `SuccessResult` and `ErrorResult` centralize notification logic
- **Endpoint Discovery**: We find the HTTP endpoint of the transmitter to call its API
- **HTTP Call**: We make a POST request to the `/online` endpoint on the transmitter
- **Error Handling**: All errors are caught and displayed using the `ErrorResult` helper

## Adding the "Take Offline" Command with Confirmation

For potentially disruptive operations, it's best practice to ask for confirmation. Let's add an offline command that shows a confirmation dialog first.

Add this code after the online command:

```csharp
// Add "Take Offline" command with confirmation dialog
transmitter.WithCommand("offline", "Take Offline", async context =>
{
    var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();
    
    if (!interactionService.IsAvailable)
    {
        return CommandResults.Failure("Interaction service not available");
    }

    try
    {
        // Show confirmation dialog
        var confirmResult = await interactionService.PromptConfirmationAsync(
            title: "Confirm Offline",
            message: $"Are you sure you want to take **{transmitter.Resource.Name}** offline?\n\nThis will stop ghost tracking for this transmitter.",
            options: new MessageBoxInteractionOptions
            {
                Intent = MessageIntent.Warning,
                PrimaryButtonText = "Take Offline",
                SecondaryButtonText = "Cancel",
                ShowSecondaryButton = true,
                EnableMessageMarkdown = true
            });

        if (confirmResult.Canceled || confirmResult.Data == false)
        {
            await interactionService.PromptNotificationAsync(
                title: "Cancelled",
                message: $"{transmitter.Resource.Name} offline operation cancelled",
                options: new NotificationInteractionOptions
                {
                    Intent = MessageIntent.Information
                });
            return CommandResults.Failure("User cancelled");
        }

        // Get HTTP endpoint for the transmitter
        var endpoint = transmitter.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.UriScheme == "http" || e.UriScheme == "https");

        if (endpoint == null)
        {
            return await ErrorResult(interactionService, $"No HTTP endpoint found for {transmitter.Resource.Name}");
        }

        // Make HTTP call to /offline endpoint
        var httpClient = new HttpClient();
        
        var port = endpoint.Port ?? (endpoint.UriScheme == "https" ? 443 : 80);
        var url = $"http://localhost:{port}/offline";
        
        var response = await httpClient.PostAsync(url, null);
        
        if (response.IsSuccessStatusCode)
        {
            return await SuccessResult(interactionService, $"{transmitter.Resource.Name} is now offline");
        }
        else
        {
            return await ErrorResult(interactionService, $"Failed to take {transmitter.Resource.Name} offline: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        return await ErrorResult(interactionService, $"Error taking {transmitter.Resource.Name} offline: {ex.Message}");
    }
});
```

### Understanding Confirmations

- **`PromptConfirmationAsync`**: Shows a modal confirmation dialog
- **Markdown Support**: Set `EnableMessageMarkdown = true` to use markdown in messages
- **Button Customization**: Specify custom text for primary and secondary buttons
- **Intent**: Warning intent shows a yellow/amber color scheme
- **Cancellation**: Check both `Canceled` and `Data` properties to handle user cancellation

## Using the Interactions in Program.cs

Now that we have our interaction extension method ready, let's use it in our AppHost.

Open `Program.cs` and update the transmitter loop to include the interactions:

```csharp
for (int i = 1; i < 4; i++)
{
    builder.AddProject<Projects.GhostTracker_Transmitter>($"GhostTracker-transmitter-{i}")
        .WithHttpEndpoint(port: 9000 + i)
        .WithEnvironment("GhostId", i.ToString())
        .WithReference(ghostManagerApi)
        .WithReference(pathfinderApi)
        .WaitFor(ghostManagerApi)
        .WaitFor(pathfinderApi)
        .AddTransmitterInteractions();  // Add this line!
}
```

The `.AddTransmitterInteractions()` call chains our extension method, adding all three commands to each transmitter.

## Testing Your Interactive Commands

Run your application and navigate to the Aspire dashboard. You should now see three new command buttons on each transmitter resource:

1. **Take Online** - Brings the transmitter online
2. **Take Offline** - Takes the transmitter offline (with confirmation)
3. **Teleport Ghost** - Teleports the ghost to a new location

### Expected Behavior

When you click **Take Online**:
- A success notification should appear at the top of the dashboard
- The ghost should start moving on the map again

When you click **Take Offline**:
- A confirmation dialog should appear asking if you're sure
- If you confirm, the transmitter stops sending location updates
- A success notification appears
- If you cancel, an information notification tells you the operation was cancelled

When you click **Teleport Ghost**:
- The ghost instantly jumps to a new random location on the map
- A success notification with ghost emoji appears

## Understanding Message Intents

The interaction service supports different message intents that affect the visual appearance:

- **Success**: Green - for successful operations
- **Error**: Red - for errors and failures
- **Warning**: Yellow/Orange - for potentially dangerous operations
- **Information**: Blue - for informational messages
- **Confirmation**: Similar to Information - for confirmation prompts

These intents help users quickly understand the nature of the message.

## Best Practices

When using the Interaction Service:

1. **Always check `IsAvailable`**: The interaction service may not be available in all contexts
2. **Use confirmations for destructive operations**: Prevent accidental actions
3. **Provide clear messages**: Use markdown for formatting when needed
4. **Choose appropriate intents**: Use Warning for confirmations, Error for failures, Success for completions
5. **Handle cancellations**: Always check if the user cancelled a dialog
6. **Use meaningful button text**: "Take Offline" is clearer than "OK"
7. **Show feedback**: Always notify the user of the operation result
8. **Extract common logic**: Use helper methods like `SuccessResult` and `ErrorResult` to keep code DRY (Don't Repeat Yourself)

## Additional Resources

- [Aspire Interaction Service Documentation](https://aspire.dev/extensibility/interaction-service/)
- [Custom Resource Commands](https://aspire.dev/fundamentals/custom-resource-commands/)
- [Message Intents and Options](https://aspire.dev/extensibility/interaction-service/#display-messages)

## Extra Challenge (Optional)

Try extending the interactions further:

1. **Add a teleport command**: Call the `/teleport` endpoint of a transmitter.
2. **Add input prompts**: Use `PromptInputAsync` to ask for a specific teleport location
3. **Add validation**: Check if the transmitter is already online/offline before executing
4. **Add custom icons**: Enhance the visual appearance of your commands
5. **Create a wizard**: Use multiple input prompts to create a multi-step configuration wizard

---

[Next Exercise →](./exercise_08.md)
