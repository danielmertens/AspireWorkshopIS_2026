# Step 9 - Getting to the cloud

With Aspire, it's easy to deploy your application locally using Docker Compose or to the cloud using Azure Container Apps. This exercise will walk you through both deployment options.

## Understanding the Deployment Manifest

Before deploying, let's understand what Aspire generates for deployment. The deployment manifest is a JSON document that describes all your application's resources, their configurations, and dependencies.

Navigate to your AppHost project directory and generate a manifest:

```powershell
cd Starter\GhostTracker.AppHost
aspire do publish-manifest --output-path ./aspire-manifest.json
```

Open the generated `aspire-manifest.json` file and inspect it. You'll see:

- **resources**: An object containing all resources from your AppHost (projects, containers, Azure resources)
- **type fields**: Each resource has a type (e.g., `project.v0`, `container.v0`, `azure.bicep.v0`)
- **bindings**: Network endpoints and ports for each service
- **env variables**: Environment configuration including connection strings and service discovery
- **connectionString placeholders**: References like `{rabbitmq1.connectionString}` that get resolved during deployment

This manifest is used by deployment tools to provision infrastructure in your target environment. More info about the manifest format can be found [here](https://aspire.dev/deployment/manifest-format/).

## Option 1: Deploy with Docker Compose

Docker Compose is perfect for local deployment, testing, or deploying to VMs.

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running
- Or [Podman](https://podman.io/getting-started/installation) as an alternative

### Add Docker Support

Add the Docker Compose hosting package to your AppHost project:

```powershell
cd Starter\GhostTracker.AppHost
aspire add docker
```

When prompted, select the `Aspire.Hosting.Docker` package.

### Configure the AppHost

Open `GhostTracker.AppHost\Program.cs` and add the Docker Compose environment at the beginning of the builder configuration:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Docker Compose environment
builder.AddDockerComposeEnvironment("env");

// ... rest of your existing resources
```

The `AddDockerComposeEnvironment` method configures Aspire to generate Docker Compose files and containerize your projects during deployment.

### Deploy to Docker Compose

Deploy your application:

```powershell
aspire deploy
```

This command will:
1. Build container images for all your projects
2. Generate Docker Compose configuration files in `aspire-output/`
3. Start all services using Docker Compose
4. Display URLs where your services are accessible

Look for output like:
```
Successfully deployed env-dashboard to http://localhost:54633.
Successfully deployed webfrontend to http://localhost:54463.
```

Open your browser to the webfrontend URL to verify the deployment.

### Verify Docker Compose Deployment

Check the generated files in the `aspire-output` directory:
- `docker-compose.yaml`: Defines all your services
- `.env.Production`: Contains container image names and ports
- `.env`: Additional environment variables

You can also view running containers:

```powershell
docker ps
```

### Clean Up Docker Compose Resources

When you're done, stop and remove all containers:

```powershell
aspire do docker-compose-down-env
```

## Option 2: Deploy to Azure Container Apps

Azure Container Apps provides a fully managed container platform with built-in scaling, monitoring, and HTTPS endpoints.

### Prerequisites

Before deploying to Azure:
- ✅ An active Azure subscription ([create free account](https://azure.microsoft.com/free/))
- ✅ [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed
- ✅ Aspire CLI installed (should already be installed from previous exercises)

⚠️ **Cost Warning**: Deploying to Azure will create resources that incur costs. Estimated cost: $1-5 per day depending on usage. **Remember to clean up resources when finished.**

### Authenticate with Azure

Log in to Azure CLI:

```powershell
az login
```

This opens a web browser for authentication. Sign in with your Azure credentials.

### Add Azure Container Apps Support

Add the Azure Container Apps hosting package:

```powershell
cd Starter\GhostTracker.AppHost
aspire add azure-appcontainers
```

Select the `Aspire.Hosting.Azure.AppContainers` package when prompted.

### Configure the AppHost for Azure

Open `GhostTracker.AppHost\Program.cs` and add the Azure Container Apps environment:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Container Apps environment
builder.AddAzureContainerAppEnvironment("aspire-env");

// ... rest of your existing resources

// Make sure your frontend has external endpoints enabled for Azure
var node = builder.AddNpmApp("react", "../GhostTracker.React")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints()  // Important for Azure!
    .PublishAsDockerFile();
```

### Deploy to Azure

Deploy your application using the Aspire CLI:

```powershell
aspire deploy
```

The deployment process is interactive and will prompt you to:

1. **Select Azure Tenant**: If you have multiple tenants
2. **Select Subscription**: Choose your Azure subscription
3. **Select or Create Resource Group**: Either select an existing one or enter a new name

The deployment will then:
1. Build container images for all projects
2. Provision Azure infrastructure (Container Registry, Container Apps Environment, Container Apps)
3. Push images to Azure Container Registry
4. Deploy your services to Azure Container Apps
5. Configure networking and HTTPS endpoints

⏱️ **Expected time**: 10-15 minutes for the first deployment

### Access Your Deployed Application

After deployment completes, the CLI output will show:

- URLs for deployed services (look for lines like `Successfully deployed webfrontend to https://...`)
- Resource group name
- Container Apps endpoints

You can also view your deployment in the Azure Portal:

1. Navigate to [https://portal.azure.com](https://portal.azure.com)
2. Go to **Resource Groups** → Find your resource group (usually `rg-<environment-name>`)
3. You'll see resources including:
   - Container Apps (your services)
   - Container Apps Environment
   - Container Registry
   - Log Analytics Workspace

Click on any Container App to view:
- Application URL
- Logs and metrics
- Environment variables
- Container configuration

### What Changed for Azure?

When deploying to Azure, Aspire automatically handles several transformations:

- **Local containers** → Azure Container Apps with auto-scaling
- **Local RabbitMQ container** → Either Azure Service Bus or RabbitMQ in Container Apps (depending on configuration)
- **Local ports** → Azure-managed HTTPS endpoints with certificates
- **Local dashboard** → Azure-hosted dashboard (separately deployed)

### Monitoring and Troubleshooting

View application logs in Azure Portal:
1. Navigate to your Container App
2. Click **Logs** in the left menu
3. Use Log Analytics queries to search logs

Or use Azure CLI:

```powershell
az containerapp logs show --name <app-name> --resource-group <resource-group> --follow
```

Common issues:
- **Authentication errors**: Run `az login` again
- **Resource naming conflicts**: Use a unique environment name
- **Container build failures**: Ensure Docker is running and projects build locally

### Clean Up Azure Resources

**IMPORTANT**: When you're finished with the workshop, delete all Azure resources to avoid ongoing costs.

Using Azure CLI:

```powershell
az group delete --name <your-resource-group-name>
```

Or using Azure Portal:
1. Go to **Resource Groups**
2. Find your resource group (e.g., `rg-ghosttracker-yourname`)
3. Click **Delete resource group**
4. Type the resource group name to confirm
5. Click **Delete**

## Summary

In this exercise, you learned:

- ✅ How to generate an Aspire deployment manifest
- ✅ How to deploy locally using Docker Compose
- ✅ How to deploy to Azure Container Apps
- ✅ How Aspire transforms local resources for cloud deployment
- ✅ How to monitor and clean up deployed resources

Aspire provides a consistent deployment experience whether you're deploying locally or to the cloud, abstracting away the complexity of container orchestration and cloud infrastructure provisioning.

## Additional Resources

- [Aspire Deployment Overview](https://aspire.dev/deployment/overview/)
- [Aspire CLI Deploy Command](https://aspire.dev/reference/cli/commands/aspire-deploy/)
- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [Deployment Manifest Format](https://aspire.dev/deployment/manifest-format/)
