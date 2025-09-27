namespace OrdersProcessor.Service.Services
{
    public class OrderProcessorConfiguration
    {
        public int ProcessingIntervalMinutes { get; set; } = 5;
        public int PendingToProcessingMinutes { get; set; } = 2;
        public int ProcessingToShippedMinutes { get; set; } = 10;
    }
}