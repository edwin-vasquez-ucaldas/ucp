using NotificationService.Models;

namespace NotificationService.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IRabbitMQService _rabbitMq;

        public NotificationService(IRabbitMQService rabbitMq)
        {
            _rabbitMq = rabbitMq;
        }

        public async Task<string> SendNotificationRequestAsync(NotificationRequest request)
        {
            if(request == null || (!request.Type.Equals("email") && !request.Type.Equals("sms")))
            {
                throw new ArgumentException("Invalid request");
            }

            string routingKey = request.Type.Equals("email") ? "notification.email" : "notification.sms";

            await _rabbitMq.PublishAsync("notification-exchange", routingKey, request);

            return request.Type;
        }
    }
}
