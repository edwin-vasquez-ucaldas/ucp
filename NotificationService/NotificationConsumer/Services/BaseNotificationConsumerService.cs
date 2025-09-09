using NotificationConsumer.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationConsumer.Services
{
    public abstract class BaseNotificationConsumerService : INotificationConsumerService, IDisposable
    {
        protected readonly ILogger _logger;
        protected IConnection? _connection;
        protected IChannel? _channel;
        protected readonly string _queueName;
        protected readonly string _routingKey;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        protected BaseNotificationConsumerService(
            ILogger logger,
            string queueName,
            string routingKey)
        {
            _logger = logger;
            _queueName = queueName;
            _routingKey = routingKey;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    Uri = new Uri("amqp://guest:guest@localhost:5672/")
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Declare exchange
                string exchangeName = "notification-exchange";
                await _channel.ExchangeDeclareAsync(
                    exchange: exchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                );

                // Declare queue
                await _channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                // Bind queue to exchange
                await _channel.QueueBindAsync(
                    queue: _queueName,
                    exchange: exchangeName,
                    routingKey: _routingKey
                );

                // Set up consumer
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        _logger.LogInformation("Received message: {Message}", message);

                        var notification = JsonSerializer.Deserialize<NotificationRequest>(message);
                        if (notification != null)
                        {
                            await ProcessNotificationAsync(notification);
                            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                            _logger.LogInformation("Successfully processed notification for {Recipient}", notification.Recipient);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);
                _logger.LogInformation("Started consuming from queue: {QueueName}", _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting notification consumer");
                throw;
            }
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cancellationTokenSource.Cancel();

                if (_channel?.IsOpen == true)
                {
                    await _channel.CloseAsync();
                }

                if (_connection?.IsOpen == true)
                {
                    await _connection.CloseAsync();
                }

                _logger.LogInformation("Stopped consuming from queue: {QueueName}", _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping notification consumer");
            }
        }

        protected abstract Task ProcessNotificationAsync(NotificationRequest notification);

        public virtual void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
