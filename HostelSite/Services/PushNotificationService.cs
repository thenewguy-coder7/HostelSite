using HostelSite.Models.Data;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace HostelSite.Services;

// Sends a browser push notification to every admin who has enabled
// notifications on the Dashboard. Used for: a new logistics booking, a new
// aesthetic request, and the "pickup in 2 days" reminder job.
public class PushNotificationService
{
    private readonly HostelDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(HostelDbContext db, IConfiguration config, ILogger<PushNotificationService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task NotifyAllAdminsAsync(string title, string body, string? url = null)
    {
        var subject    = _config["Vapid:Subject"]?.Trim();
        var publicKey  = _config["Vapid:PublicKey"]?.Trim();
        var privateKey = _config["Vapid:PrivateKey"]?.Trim();

        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(subject))
        {
            _logger.LogWarning("Push notification skipped — Vapid keys are not configured.");
            return;
        }

        var subscriptions = await _db.AdminPushSubscriptions.ToListAsync();
        if (subscriptions.Count == 0) return;

        var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        var client = new WebPushClient();

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            title,
            body,
            url = url ?? "/Admin/Dashboard"
        });

        var expired = new List<AdminPushSubscription>();

        foreach (var sub in subscriptions)
        {
            var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
            try
            {
                await client.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (WebPushException ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // The browser revoked this subscription (uninstalled, cleared
                // site data, permission withdrawn, etc.) — stop trying it.
                expired.Add(sub);
            }
            catch (Exception ex)
            {
                // Never let a bad push subscription break the booking/request
                // flow that triggered this notification.
                _logger.LogWarning(ex, "Failed to send push notification to subscription {Id}", sub.SubscriptionId);
            }
        }

        if (expired.Count > 0)
        {
            _db.AdminPushSubscriptions.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }
    }
}
