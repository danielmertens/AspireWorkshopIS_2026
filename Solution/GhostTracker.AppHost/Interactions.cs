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

        // Add "Teleport" command
        transmitter.WithCommand("teleport", "Teleport Ghost", async context =>
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
                    return await ErrorResult(interactionService, $"No HTTP endpoint found for {transmitter.Resource.Name}");
                }

                // Make HTTP call to /teleport endpoint
                var httpClient = new HttpClient();
                
                var port = endpoint.Port ?? (endpoint.UriScheme == "https" ? 443 : 80);
                var url = $"http://localhost:{port}/teleport";
                
                var response = await httpClient.PostAsync(url, null);
                
                if (response.IsSuccessStatusCode)
                {
                    return await SuccessResult(interactionService, $"{transmitter.Resource.Name} teleported successfully 👻✨");
                }
                else
                {
                    return await ErrorResult(interactionService, $"Failed to teleport {transmitter.Resource.Name}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return await ErrorResult(interactionService, $"Error taking {transmitter.Resource.Name} offline: {ex.Message}");
            }
        });

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
