using NotificationConsumer.Models;

namespace NotificationConsumer.Services
{
    public interface IEmailNotificationService
    {
        Task SendEmailAsync(NotificationRequest notification);
    }
}
