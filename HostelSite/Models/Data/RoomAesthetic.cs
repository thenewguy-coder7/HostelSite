using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class RoomAesthetic
{
    public int AestheticId { get; set; }

    public string ThemeName { get; set; } = null!;

    public string StyleCategory { get; set; } = null!;

    public string? Description { get; set; }

    public decimal AdditionalCost { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AestheticRequest> AestheticRequests { get; set; } = new List<AestheticRequest>();
}
