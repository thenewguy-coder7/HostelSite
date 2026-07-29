using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class LogisticsItem
{
    public int ItemId { get; set; }

    public string ItemName { get; set; } = null!;

    public string Category { get; set; } = null!;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
