using RabbitMQ.Client;
using System.Text;

namespace GhostTracker.Transmitter.RabbitMQ.Publishers
{
    public abstract class AbstractPublisher
    {
        private readonly IConnection _rabbitConnection;

        public AbstractPublisher(IConnection rabbitConnection)
        {
            _rabbitConnection = rabbitConnection;
        }

        protected async Task SendMessageAsync(string message, string queue)
        {
            var body = Encoding.UTF8.GetBytes(message);

            var channel = await _rabbitConnection.CreateChannelAsync();
            await channel.BasicPublishAsync(exchange: "amq.direct",
                routingKey: queue,
                body: body);
        }
    }
}
