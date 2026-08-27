using HostelSite.Models.Data;
using HostelSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HostelSite.Controllers
{
    // Endpoint for an external scheduler (the app itself has no built-in cron —
    // see the "2 days before pickup" reminder setup notes) to trigger the
    // "pickup in 2 days" admin reminder. Protected by a shared secret instead
    // of a login, since it's called by a scheduler with no user session.
    public class NotificationsController : Controller
    {
        private readonly HostelDbContext _db;
        private readonly IConfiguration _config;
        private readonly PushNotificationService _push;

        public NotificationsController(HostelDbContext db, IConfiguration config, PushNotificationService push)
        {
            _db = db;
            _config = config;
            _push = push;
        }

        // GET /Notifications/RunReminders?key=...
        [HttpGet]
        public async Task<IActionResult> RunReminders(string? key)
        {
            var expected = _config["Notifications:CronSecret"];
            if (string.IsNullOrEmpty(expected) || key != expected)
                return Unauthorized();

            var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

            var dueOrders = await _db.LogisticsOrders
                .Where(o => !o.IsDeleted
                         && o.PickupDate == targetDate
                         && o.PickupReminderSentAt == null)
                .Include(o => o.Student)
                .ToListAsync();

            foreach (var order in dueOrders)
            {
                var studentName = order.Student != null
                    ? $"{order.Student.FirstName} {order.Student.LastName}"
                    : "A student";

                await _push.NotifyAllAdminsAsync(
                    "Pickup in 2 days",
                    $"Order #{order.OrderId} for {studentName} is due for pickup on {order.PickupDate:dd MMM yyyy}.",
                    "/Admin/Dashboard");

                order.PickupReminderSentAt = DateTime.UtcNow;
            }

            if (dueOrders.Count > 0)
                await _db.SaveChangesAsync();

            return Json(new { checkedFor = targetDate.ToString("yyyy-MM-dd"), remindersSent = dueOrders.Count });
        }
    }
}
