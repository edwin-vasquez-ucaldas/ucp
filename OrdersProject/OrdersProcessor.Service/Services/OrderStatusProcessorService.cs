using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrdersProcessor.Service.Data;
using OrdersProcessor.Service.Models;

namespace OrdersProcessor.Service.Services
{
    public class OrderStatusProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderStatusProcessorService> _logger;
        private readonly OrderProcessorConfiguration _configuration;

        public OrderStatusProcessorService(
            IServiceProvider serviceProvider,
            ILogger<OrderStatusProcessorService> logger,
            IOptions<OrderProcessorConfiguration> configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Status Processor Service started");
            _logger.LogInformation("Configuration: Processing Interval = {ProcessingInterval} minutes, " +
                                 "Pending to Processing = {PendingToProcessing} minutes, " +
                                 "Processing to Shipped = {ProcessingToShipped} minutes",
                _configuration.ProcessingIntervalMinutes,
                _configuration.PendingToProcessingMinutes,
                _configuration.ProcessingToShippedMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOrdersAsync();

                    // Wait for the configured interval before the next execution
                    await Task.Delay(TimeSpan.FromMinutes(_configuration.ProcessingIntervalMinutes), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing orders");

                    // Wait for a shorter period before retrying on error
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Order Status Processor Service stopped");
        }

        private async Task ProcessOrdersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var currentTime = DateTime.UtcNow;
            var pendingCutoffTime = currentTime.AddMinutes(-_configuration.PendingToProcessingMinutes);
            var processingCutoffTime = currentTime.AddMinutes(-_configuration.ProcessingToShippedMinutes);

            _logger.LogInformation("Starting order processing cycle at {CurrentTime}", currentTime);

            // Track statistics
            int pendingToProcessingCount = 0;
            int processingToShippedCount = 0;

            try
            {
                // Process Pending to Processing
                var pendingOrders = await context.Orders
                    .Where(o => o.Status == OrderStatus.Pending && o.OrderDate <= pendingCutoffTime)
                    .ToListAsync();

                foreach (var order in pendingOrders)
                {
                    var timeSinceOrder = currentTime - order.OrderDate;

                    _logger.LogInformation("Processing order {OrderId} from Pending to Processing. " +
                                         "Order created {OrderDate}, {MinutesAgo} minutes ago",
                        order.Id, order.OrderDate, Math.Round(timeSinceOrder.TotalMinutes, 1));

                    order.Status = OrderStatus.Processing;
                    pendingToProcessingCount++;
                }

                // Process Processing to Shipped
                var processingOrders = await context.Orders
                    .Where(o => o.Status == OrderStatus.Processing && o.OrderDate <= processingCutoffTime)
                    .ToListAsync();

                foreach (var order in processingOrders)
                {
                    var timeSinceOrder = currentTime - order.OrderDate;

                    _logger.LogInformation("Processing order {OrderId} from Processing to Shipped. " +
                                         "Order created {OrderDate}, {MinutesAgo} minutes ago",
                        order.Id, order.OrderDate, Math.Round(timeSinceOrder.TotalMinutes, 1));

                    order.Status = OrderStatus.Shipped;
                    order.ShippedDate = currentTime;
                    processingToShippedCount++;
                }

                // Save all changes
                if (pendingToProcessingCount > 0 || processingToShippedCount > 0)
                {
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Order processing cycle completed successfully. " +
                                         "Updated {PendingCount} orders from Pending to Processing, " +
                                         "{ProcessingCount} orders from Processing to Shipped",
                        pendingToProcessingCount, processingToShippedCount);
                }
                else
                {
                    _logger.LogInformation("Order processing cycle completed. No orders required status updates.");
                }

                // Log current order statistics
                await LogOrderStatisticsAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during order processing cycle");
                throw;
            }
        }

        private async Task LogOrderStatisticsAsync(OrderDbContext context)
        {
            try
            {
                var statistics = await context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var statsMessage = string.Join(", ",
                    statistics.Select(s => $"{s.Status}: {s.Count}"));

                _logger.LogInformation("Current order statistics: {Statistics}", statsMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve order statistics");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Status Processor Service is stopping...");
            await base.StopAsync(stoppingToken);
        }
    }
}