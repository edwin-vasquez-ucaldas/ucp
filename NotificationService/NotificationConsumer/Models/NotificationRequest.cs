namespace NotificationConsumer.Models
{
    public class NotificationRequest
    {
        public string Type { get; set; }
        public string Recipient { get; set; }
        public string Message { get; set; }
        public string Subject { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
