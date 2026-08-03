using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class AestheticRequest
{
    public int RequestId { get; set; }

    public int StudentId { get; set; }

    public int? RoomId { get; set; }

    public int? AestheticId { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual RoomAesthetic? Aesthetic { get; set; }


    public virtual Student Student { get; set; } = null!;
}
