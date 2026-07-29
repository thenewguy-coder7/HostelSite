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

    public virtual LogisticsItem Item { get; set; } = null!;

    public virtual LogisticsOrder Order { get; set; } = null!;
}
