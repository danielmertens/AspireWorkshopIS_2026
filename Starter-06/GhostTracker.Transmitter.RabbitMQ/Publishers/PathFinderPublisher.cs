using GhostTracker.Transmitter.RabbitMQ.Models;
using RabbitMQ.Client;
using System.Text.Json;

namespace GhostTracker.Transmitter.RabbitMQ.Publishers
{
    public interface IPathFinderPublisher
    {
        Task TransmitLocationAsync(int ghostId, GhostLocation location);
    }

    public class PathFinderPublisher : AbstractPublisher, IPathFinderPublisher
    {
        public PathFinderPublisher(IConnection rabbitConnection)
            : base(rabbitConnection)
        {
        }

        public async Task TransmitLocationAsync(int ghostId, GhostLocation location)
        {
            var evt = new LocationEvent
            {
                GhostId = ghostId,
                Coordinate = location.Coordinate,
                Heading = location.Heading
            };

            await SendMessageAsync(JsonSerializer.Serialize(evt), Constants.LocationQueue);
        }
    }

    public class LocationEvent
    {
        public int GhostId { get; set; }
        public required Coordinate Coordinate { get; set; }
        public required Heading Heading { get; set; }
    }
}
