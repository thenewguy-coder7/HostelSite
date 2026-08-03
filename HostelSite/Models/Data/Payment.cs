using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? BookingId { get; set; }

    public string PaystackReference { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string? PaymentChannel { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

}
