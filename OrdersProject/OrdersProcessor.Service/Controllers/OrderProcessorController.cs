using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersProcessor.Service.Data;
using OrdersProcessor.Service.Models;

namespace OrdersProcessor.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderProcessorController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderProcessorController> _logger;

        public OrderProcessorController(OrderDbContext context, ILogger<OrderProcessorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get service health status
        /// </summary>
        [HttpGet("health")]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "Order Status Processor",
                Timestamp = DateTime.UtcNow,
                Message = "Background service is running"
            });
        }

        /// <summary>
        /// Get current order statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetOrderStatistics()
        {
            try
            {
                var statistics = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count(),
                        TotalAmount = g.Sum(o => o.TotalAmount)
                    })
                    .ToListAsync();

                var totalOrders = await _context.Orders.CountAsync();
                var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

                return Ok(new
                {
                    TotalOrders = totalOrders,
                    TotalRevenue = totalRevenue,
                    StatusBreakdown = statistics,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order statistics");
                return StatusCode(500, "Error retrieving statistics");
            }
        }

        /// <summary>
        /// Get orders that are eligible for status updates
        /// </summary>
        [HttpGet("eligible-orders")]
        public async Task<IActionResult> GetEligibleOrders()
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var pendingCutoffTime = currentTime.AddMinutes(-2); // 2 minutes for Pending to Processing
                var processingCutoffTime = currentTime.AddMinutes(-10); // 10 minutes for Processing to Shipped

                var pendingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending && o.OrderDate <= pendingCutoffTime)
                    .Select(o => new
                    {
                        o.Id,
                        o.CustomerName,
                        o.Status,
                        o.OrderDate,
                        MinutesOld = Math.Round((currentTime - o.OrderDate).TotalMinutes, 1),
                        NextStatus = "Processing"
                    })
                    .ToListAsync();

                var processingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Processing && o.OrderDate <= processingCutoffTime)
                    .Select(o => new
                    {
                        o.Id,
                        o.CustomerName,
                        o.Status,
                        o.OrderDate,
                        MinutesOld = Math.Round((currentTime - o.OrderDate).TotalMinutes, 1),
                        NextStatus = "Shipped"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    PendingToProcessing = pendingOrders,
                    ProcessingToShipped = processingOrders,
                    TotalEligible = pendingOrders.Count + processingOrders.Count,
                    CheckTime = currentTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving eligible orders");
                return StatusCode(500, "Error retrieving eligible orders");
            }
        }

        /// <summary>
        /// Manually trigger order processing (for testing)
        /// </summary>
        [HttpPost("process-now")]
        public async Task<IActionResult> ProcessOrdersNow()
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var pendingCutoffTime = currentTime.AddMinutes(-2);
                var processingCutoffTime = currentTime.AddMinutes(-10);

                int pendingToProcessingCount = 0;
                int processingToShippedCount = 0;

                // Process Pending to Processing
                var pendingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending && o.OrderDate <= pendingCutoffTime)
                    .ToListAsync();

                foreach (var order in pendingOrders)
                {
                    order.Status = OrderStatus.Processing;
                    pendingToProcessingCount++;
                }

                // Process Processing to Shipped
                var processingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Processing && o.OrderDate <= processingCutoffTime)
                    .ToListAsync();

                foreach (var order in processingOrders)
                {
                    order.Status = OrderStatus.Shipped;
                    order.ShippedDate = currentTime;
                    processingToShippedCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Manual order processing completed. " +
                                     "Updated {PendingCount} pending orders, {ProcessingCount} processing orders",
                    pendingToProcessingCount, processingToShippedCount);

                return Ok(new
                {
                    Message = "Order processing completed successfully",
                    PendingToProcessing = pendingToProcessingCount,
                    ProcessingToShipped = processingToShippedCount,
                    TotalProcessed = pendingToProcessingCount + processingToShippedCount,
                    ProcessedAt = currentTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual order processing");
                return StatusCode(500, "Error processing orders");
            }
        }
    }
}