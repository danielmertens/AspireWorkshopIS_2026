using RabbitMQ.Client;
using System.Text.Json;

namespace GhostTracker.Transmitter.RabbitMQ.Publishers
{
    public interface IGhostManagerPublisher
    {
        Task BringOnlineAsync(int ghostId);
        Task TakeOfflineAsync(int ghostId);
    }

    public class GhostManagerPublisher : AbstractPublisher, IGhostManagerPublisher
    {
        public GhostManagerPublisher(IConnection rabbitConnection)
            : base(rabbitConnection)
        {
        }

        public async Task BringOnlineAsync(int ghostId)
        {
            var evt = new StatusChangedEvent()
            {
                GhostId = ghostId,
                Online = true
            };

            await SendMessageAsync(JsonSerializer.Serialize(evt), Constants.StatusQueue);
        }

        public async Task TakeOfflineAsync(int ghostId)
        {
            var evt = new StatusChangedEvent()
            {
                GhostId = ghostId,
                Online = false
            };

            await SendMessageAsync(JsonSerializer.Serialize(evt), Constants.StatusQueue);
        }
    }

    public class StatusChangedEvent
    {
        public int GhostId { get; set; }
        public bool Online { get; set; }
    }
}
