namespace NotificationService.Services
{
    public interface IRabbitMQService
    {
        Task PublishAsync<T>(string exchangeName, string routingKey, T message);
    }
}
