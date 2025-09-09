using NotificationService.Models;

namespace NotificationService.Services
{
    public interface INotificationService
    {
        Task<string> SendNotificationRequestAsync(NotificationRequest request);
    }
}
