using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class Booking
{
    public int BookingId { get; set; }

    public int StudentId { get; set; }

    public int RoomId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public string BookingStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Room Room { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
