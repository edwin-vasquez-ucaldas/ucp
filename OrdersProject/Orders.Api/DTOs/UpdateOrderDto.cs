using System.ComponentModel.DataAnnotations;
using Orders.Api.Models;

namespace Orders.Api.DTOs
{
    public class UpdateOrderDto
    {
        [StringLength(100)]
        public string? CustomerName { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string? CustomerEmail { get; set; }

        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        public decimal? TotalAmount { get; set; }

        public OrderStatus? Status { get; set; }

        public DateTime? ShippedDate { get; set; }

        public DateTime? DeliveredDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}