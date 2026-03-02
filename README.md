# .NET Aspire Workshop

A hands-on workshop for learning .NET Aspire by building and deploying **Ghost Tracker**, a distributed application for monitoring paranormal activity.

## About This Workshop

This workshop teaches you how to use .NET Aspire to build, orchestrate, and deploy cloud-ready distributed applications. Through a series of progressive exercises, you'll work with a realistic microservices architecture featuring service discovery, message queues, observability, and cloud deployment.

### What You'll Learn

- Service orchestration with .NET Aspire AppHost
- Inter-service communication and service discovery
- Integrating third-party services (RabbitMQ)
- OpenTelemetry and distributed tracing
- Deploying .NET Aspire applications to Azure

## The Ghost Tracker Application

Ghost Tracker is a distributed application that monitors real-time ghost locations. The application includes:

- **React Frontend**: Real-time map displaying ghost locations
- **BFF (Backend for Frontend)**: ASP.NET Web API aggregating backend services
- **GhostManager**: Service managing ghost registry and metadata
- **PathFinder**: Service tracking real-time location data
- **Transmitter**: Background service simulating IoT devices
- **Transmitter.RabbitMQ**: Alternative message queue-based transmitter

## Prerequisites

Before starting, ensure you have:

- **.NET 10.0 SDK or later** - [Download here](https://dotnet.microsoft.com/download)
- **Docker Desktop** - Required for running containers
- **IDE**: Visual Studio 2022 (17.10+), VS Code with C# Dev Kit, or JetBrains Rider (2024.1+)
- **.NET Aspire workload** - See installation instructions in [exercise_00.md](exercise_00.md)

## Getting Started

1. **Clone this repository**
   ```bash
   git clone <repository-url>
   cd AspireWorkshop
   ```

2. **Start with the exercises**
   - Begin with [exercise_00.md](Exercises/exercise_00.md) for environment setup
   - Work through exercises sequentially (00-09)
   - Each exercise builds on the previous one

3. **Choose your path**
   - Work in the `Starter/` folder to follow along with the exercises
   - Use a pre-built checkpoint folder (`Starter-03`, `Starter-06`) to jump ahead to a specific exercise
   - Reference the `Solution/` folder if you get stuck or want to see the completed code

## Workshop Structure

| Exercise | Topic |
|----------|-------|
| [00](Exercises/exercise_00.md) | Environment setup and prerequisites |
| [01](Exercises/exercise_01.md) | Project exploration and AppHost creation |
| [02](Exercises/exercise_02.md) | Let's define our infrastructure |
| [03](Exercises/exercise_03.md) | Setting service defaults |
| [04](Exercises/exercise_04.md) | Service discovery |
| [05](Exercises/exercise_05.md) | Time to find some ghosts |
| [06](Exercises/exercise_06.md) | Integrations |
| [07](Exercises/exercise_07.md) | Interactive Commands with Aspire Interaction Service |
| [08](Exercises/exercise_08.md) | Testing Your Aspire Application |
| [09](Exercises/exercise_09.md) | Getting to the cloud |

## Repository Structure

```
AspireWorkshop/
├── Exercises/            # Workshop exercises and images
│   ├── exercise_*.md     # Step-by-step exercise instructions
│   └── Images/           # Diagrams and screenshots
├── Starter/              # Starting point – begin here for exercise 01
├── Starter-03/           # Checkpoint – begin here for exercise 03 (exercises 01-02 completed)
├── Starter-06/           # Checkpoint – begin here for exercise 06 (exercises 01-05 completed)
└── Solution/             # Complete solution for reference
```

## Support

If you encounter issues during the workshop:
- Refer to the `Solution/` folder for working code
- Check the [.NET Aspire documentation](https://aspire.dev/docs/)
- Review error messages in the Aspire dashboard

Happy ghost tracking! 👻
