using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class Amenity
{
    public int AmenityId { get; set; }

    public int RoomId { get; set; }

    public string AmenityName { get; set; } = null!;

    public string AmenityType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Room Room { get; set; } = null!;
}
