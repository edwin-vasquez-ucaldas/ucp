namespace NotificationConsumer.Services
{
    public class EmailNotificationBackgroundService : BackgroundService
    {
        private readonly EmailNotificationService _emailService;
        private readonly ILogger<EmailNotificationBackgroundService> _logger;

        public EmailNotificationBackgroundService(
            EmailNotificationService emailService,
            ILogger<EmailNotificationBackgroundService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Notification Background Service started");

            try
            {
                await _emailService.StartAsync(stoppingToken);

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Email Notification Background Service was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email Notification Background Service encountered an error");
            }
            finally
            {
                await _emailService.StopAsync(CancellationToken.None);
                _logger.LogInformation("Email Notification Background Service stopped");
            }
        }
    }
}
