using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class Admin
{
    public int AdminId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    // "Admin" = full access, including money/logistics.
    // "Staff" = aesthetics bookings only, no financial data — see StaffController.
    public string Role { get; set; } = "Admin";

    public virtual ICollection<AdminPushSubscription> PushSubscriptions { get; set; } = new List<AdminPushSubscription>();
}
