using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    // URL of the photo the student took/uploaded of the actual item they're
    // bringing in — shown to Admin/Staff so they know what to expect at
    // pickup. Optional — not every item line will have one.
    public string? PhotoUrl { get; set; }

    public virtual LogisticsItem Item { get; set; } = null!;

    public virtual LogisticsOrder Order { get; set; } = null!;
}
