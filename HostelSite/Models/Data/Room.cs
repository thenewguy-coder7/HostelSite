using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class Room
{
    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public string RoomType { get; set; } = null!;

    public int FloorNumber { get; set; }

    public int Capacity { get; set; }

    public decimal PricePerSemester { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AestheticRequest> AestheticRequests { get; set; } = new List<AestheticRequest>();

    public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
