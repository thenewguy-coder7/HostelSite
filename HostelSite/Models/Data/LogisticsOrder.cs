using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class LogisticsOrder
{
    public int OrderId { get; set; }

    public int StudentId { get; set; }

    public string OrderStatus { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public string? DeliveryNotes { get; set; }

    public DateTime OrderedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateOnly? PickupDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Student Student { get; set; } = null!;
}
