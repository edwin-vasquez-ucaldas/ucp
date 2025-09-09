using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace NotificationService.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = configuration.GetConnectionString("RabbitMQ") ?? "localhost",
                Port = configuration.GetValue<int>("RabbitMQ:Port", 5672),
                UserName = configuration.GetValue<string>("RabbitMQ:Username") ?? "guest",
                Password = configuration.GetValue<string>("RabbitMQ:Password") ?? "guest"
            };

            try
            {
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                // Declare the exchange for notifications
                _channel.ExchangeDeclareAsync(
                    exchange: "notification-exchange",
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false).GetAwaiter().GetResult();

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish RabbitMQ connection");
                throw;
            }
        }

        public async Task PublishAsync<T>(string exchangeName, string routingKey, T message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    MessageId = Guid.NewGuid().ToString()
                };

                await _channel.BasicPublishAsync(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    body: body);

                _logger.LogInformation($"Message published to exchange: {exchangeName}, routing key: {routingKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish message to exchange: {exchangeName}");
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
