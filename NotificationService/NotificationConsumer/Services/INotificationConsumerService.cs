namespace NotificationConsumer.Services
{
    public interface INotificationConsumerService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
