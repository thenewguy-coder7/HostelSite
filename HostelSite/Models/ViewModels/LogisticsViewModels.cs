using System.ComponentModel.DataAnnotations;

namespace HostelSite.ViewModels
{
    // ── CATALOG item shown on Logistics/Index ──
    public class LogisticsItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Category { get; set; }
        public string? Icon { get; set; }
    }

    // ── ORDER LINE from sessionStorage / checkout ──
    public class OrderLineViewModel
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Icon { get; set; }
        public decimal LineTotal => Price * Quantity;
    }

    // ── CHECKOUT (posted to PaymentsController) ──
    public class CheckoutViewModel
    {
        [Required]
        public List<OrderLineViewModel> Items { get; set; } = new();

        [Required(ErrorMessage = "Please select a pickup date")]
        [DataType(DataType.Date)]
        public DateTime PickupDate { get; set; }

        [Required(ErrorMessage = "Please select a return date")]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; }

        public decimal Total => Items.Sum(i => i.LineTotal);
    }

    // ── MY ORDERS list item ──
    public class LogisticsOrderSummaryViewModel
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PickupDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public List<OrderItemDetailViewModel> OrderItems { get; set; } = new();
    }

    // ── ORDER ITEM detail (inside an expanded order card) ──
    public class OrderItemDetailViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
