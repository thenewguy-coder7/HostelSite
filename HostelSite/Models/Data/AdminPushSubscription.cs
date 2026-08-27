using System;

namespace HostelSite.Models.Data;

// One browser push subscription for one admin. An admin can have more than
// one (e.g. phone + laptop), so notifications go out to every device they've
// enabled them on.
public partial class AdminPushSubscription
{
    public int SubscriptionId { get; set; }

    public int AdminId { get; set; }

    public string Endpoint { get; set; } = null!;

    public string P256dh { get; set; } = null!;

    public string Auth { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}
