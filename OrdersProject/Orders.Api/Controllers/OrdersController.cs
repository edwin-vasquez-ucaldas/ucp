using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.DTOs;
using Orders.Api.Models;

namespace Orders.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OrderDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all orders with optional filtering by status
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Page size for pagination (default: 10, max: 100)</param>
        /// <returns>List of orders</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderSummaryDto>>> GetOrders(
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validate pagination parameters
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Min(100, Math.Max(1, pageSize));

                var query = _context.Orders.Include(o => o.OrderItems).AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(o => o.Status == status.Value);
                }

                var totalCount = await query.CountAsync();
                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderSummaryDto
                    {
                        Id = o.Id,
                        CustomerName = o.CustomerName,
                        CustomerEmail = o.CustomerEmail,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        StatusName = o.Status.ToString(),
                        OrderDate = o.OrderDate,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                Response.Headers.Append("X-Page-Number", pageNumber.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

        /// <summary>
        /// Gets a specific order by ID
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Order details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    CustomerName = order.CustomerName,
                    CustomerEmail = order.CustomerEmail,
                    ShippingAddress = order.ShippingAddress,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    StatusName = order.Status.ToString(),
                    OrderDate = order.OrderDate,
                    ShippedDate = order.ShippedDate,
                    DeliveredDate = order.DeliveredDate,
                    Notes = order.Notes,
                    OrderItems = order.OrderItems.Select(item => new OrderItemDto
                    {
                        Id = item.Id,
                        ProductName = item.ProductName,
                        ProductSku = item.ProductSku,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
                };

                return Ok(orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", id);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="createOrderDto">Order creation data</param>
        /// <returns>Created order</returns>
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var order = new Order
                {
                    CustomerName = createOrderDto.CustomerName,
                    CustomerEmail = createOrderDto.CustomerEmail,
                    ShippingAddress = createOrderDto.ShippingAddress,
                    TotalAmount = createOrderDto.TotalAmount,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow,
                    Notes = createOrderDto.Notes,
                    OrderItems = createOrderDto.OrderItems.Select(item => new OrderItem
                    {
                        ProductName = item.ProductName,
                        ProductSku = item.ProductSku,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Reload the order with items to get the generated IDs
                await _context.Entry(order).Collection(o => o.OrderItems).LoadAsync();

                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    CustomerName = order.CustomerName,
                    CustomerEmail = order.CustomerEmail,
                    ShippingAddress = order.ShippingAddress,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    StatusName = order.Status.ToString(),
                    OrderDate = order.OrderDate,
                    ShippedDate = order.ShippedDate,
                    DeliveredDate = order.DeliveredDate,
                    Notes = order.Notes,
                    OrderItems = order.OrderItems.Select(item => new OrderItemDto
                    {
                        Id = item.Id,
                        ProductName = item.ProductName,
                        ProductSku = item.ProductSku,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
                };

                _logger.LogInformation("Created new order {OrderId} for customer {CustomerName}", order.Id, order.CustomerName);

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, "An error occurred while creating the order");
            }
        }

        /// <summary>
        /// Updates an existing order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="updateOrderDto">Order update data</param>
        /// <returns>Updated order</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<OrderDto>> UpdateOrder(int id, UpdateOrderDto updateOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(updateOrderDto.CustomerName))
                    order.CustomerName = updateOrderDto.CustomerName;

                if (!string.IsNullOrEmpty(updateOrderDto.CustomerEmail))
                    order.CustomerEmail = updateOrderDto.CustomerEmail;

                if (!string.IsNullOrEmpty(updateOrderDto.ShippingAddress))
                    order.ShippingAddress = updateOrderDto.ShippingAddress;

                if (updateOrderDto.TotalAmount.HasValue)
                    order.TotalAmount = updateOrderDto.TotalAmount.Value;

                if (updateOrderDto.Status.HasValue)
                {
                    var oldStatus = order.Status;
                    order.Status = updateOrderDto.Status.Value;

                    // Set dates based on status changes
                    if (order.Status == OrderStatus.Shipped && oldStatus != OrderStatus.Shipped)
                        order.ShippedDate = DateTime.UtcNow;

                    if (order.Status == OrderStatus.Delivered && oldStatus != OrderStatus.Delivered)
                        order.DeliveredDate = DateTime.UtcNow;
                }

                if (updateOrderDto.ShippedDate.HasValue)
                    order.ShippedDate = updateOrderDto.ShippedDate;

                if (updateOrderDto.DeliveredDate.HasValue)
                    order.DeliveredDate = updateOrderDto.DeliveredDate;

                if (updateOrderDto.Notes != null)
                    order.Notes = updateOrderDto.Notes;

                await _context.SaveChangesAsync();

                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    CustomerName = order.CustomerName,
                    CustomerEmail = order.CustomerEmail,
                    ShippingAddress = order.ShippingAddress,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    StatusName = order.Status.ToString(),
                    OrderDate = order.OrderDate,
                    ShippedDate = order.ShippedDate,
                    DeliveredDate = order.DeliveredDate,
                    Notes = order.Notes,
                    OrderItems = order.OrderItems.Select(item => new OrderItemDto
                    {
                        Id = item.Id,
                        ProductName = item.ProductName,
                        ProductSku = item.ProductSku,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
                };

                _logger.LogInformation("Updated order {OrderId}", id);

                return Ok(orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
                return StatusCode(500, "An error occurred while updating the order");
            }
        }

        /// <summary>
        /// Updates only the status of an order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="status">New status</param>
        /// <returns>Updated order</returns>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int id, [FromBody] OrderStatus status)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                var oldStatus = order.Status;
                order.Status = status;

                // Set dates based on status changes
                if (status == OrderStatus.Shipped && oldStatus != OrderStatus.Shipped)
                    order.ShippedDate = DateTime.UtcNow;

                if (status == OrderStatus.Delivered && oldStatus != OrderStatus.Delivered)
                    order.DeliveredDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    CustomerName = order.CustomerName,
                    CustomerEmail = order.CustomerEmail,
                    ShippingAddress = order.ShippingAddress,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    StatusName = order.Status.ToString(),
                    OrderDate = order.OrderDate,
                    ShippedDate = order.ShippedDate,
                    DeliveredDate = order.DeliveredDate,
                    Notes = order.Notes,
                    OrderItems = order.OrderItems.Select(item => new OrderItemDto
                    {
                        Id = item.Id,
                        ProductName = item.ProductName,
                        ProductSku = item.ProductSku,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
                };

                _logger.LogInformation("Updated order {OrderId} status from {OldStatus} to {NewStatus}", id, oldStatus, status);

                return Ok(orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
                return StatusCode(500, "An error occurred while updating the order status");
            }
        }

        /// <summary>
        /// Deletes an order (soft delete by setting status to Cancelled)
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);

                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                // Soft delete by setting status to Cancelled instead of hard delete
                if (order.Status == OrderStatus.Delivered)
                {
                    return BadRequest("Cannot delete a delivered order");
                }

                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cancelled (soft deleted) order {OrderId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                return StatusCode(500, "An error occurred while deleting the order");
            }
        }

        /// <summary>
        /// Hard deletes an order (removes from database completely)
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}/hard")]
        public async Task<IActionResult> HardDeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                if (order.Status == OrderStatus.Delivered)
                {
                    return BadRequest("Cannot hard delete a delivered order");
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Hard deleted order {OrderId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting order {OrderId}", id);
                return StatusCode(500, "An error occurred while hard deleting the order");
            }
        }

        /// <summary>
        /// Gets order statistics
        /// </summary>
        /// <returns>Order statistics</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult> GetOrderStatistics()
        {
            try
            {
                var stats = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count(), TotalAmount = g.Sum(o => o.TotalAmount) })
                    .ToListAsync();

                var totalOrders = await _context.Orders.CountAsync();
                var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

                var result = new
                {
                    TotalOrders = totalOrders,
                    TotalRevenue = totalRevenue,
                    StatusBreakdown = stats.Select(s => new
                    {
                        Status = s.Status.ToString(),
                        Count = s.Count,
                        TotalAmount = s.TotalAmount,
                        Percentage = totalOrders > 0 ? (double)s.Count / totalOrders * 100 : 0
                    })
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order statistics");
                return StatusCode(500, "An error occurred while retrieving order statistics");
            }
        }
    }
}