using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace GhostTracker.GhostManager.Services
{
    public class RabbitMqListener : BackgroundService
    {
        private readonly IConnection _rabbitConnection;
        private readonly IServiceScopeFactory _serviceProvider;

        public RabbitMqListener(IConnection rabbitConnection, IServiceScopeFactory serviceProvider)
        {
            _rabbitConnection = rabbitConnection;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = await _rabbitConnection.CreateChannelAsync();
            var exchangeName = "amq.direct";
            var queueName = "StatusQueue";
            var routingKeyName = "StatusQueue";

            await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: routingKeyName);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var data = new
                {
                    ea.Exchange,
                    ea.RoutingKey,
                    Message = JsonSerializer.Deserialize<StatusChangedEvent>(ea.Body.ToArray())
                };

                using(var scope = _serviceProvider.CreateScope())
                {
                    var commandService = scope.ServiceProvider.GetService<IGhostCommandService>();
                    commandService.SetGhostStatus(data.Message.GhostId, data.Message.Online);
                }
                
                await Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

            stoppingToken.Register(() =>
            {
                channel.CloseAsync().Wait();
                channel.Dispose();
            });

            // Prevents the service from exiting immediately
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }

    public class StatusChangedEvent
    {
        public int GhostId { get; set; }
        public bool Online { get; set; }
    }
}
