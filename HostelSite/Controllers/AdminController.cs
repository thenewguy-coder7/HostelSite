using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HostelSite.Models.Data;
using HostelSite.Services;

namespace HostelSite.Controllers
{
    public class AdminController : Controller
    {
        private readonly HostelDbContext _db;
        private readonly IConfiguration _config;
        private readonly PushNotificationService _push;

        public AdminController(HostelDbContext db, IConfiguration config, PushNotificationService push)
        {
            _db = db;
            _config = config;
            _push = push;
        }

        // ── GET /Admin/Login ──
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (result.Succeeded)
                return RedirectToRoleHome(result);
            return View();
        }

        // ── POST /Admin/Login ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewData["Error"] = "Please enter your email and password.";
                return View();
            }

            var admin = _db.Admins
                .FirstOrDefault(a => a.Email == email.Trim().ToLower());

            if (admin == null || !VerifyPassword(password, admin.PasswordHash))
            {
                ViewData["Error"] = "Incorrect email or password.";
                return View();
            }

            var role = string.IsNullOrWhiteSpace(admin.Role) ? "Admin" : admin.Role;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name,           admin.FullName),
                new Claim(ClaimTypes.Email,          admin.Email),
                new Claim(ClaimTypes.Role,           role)
            };

            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AdminCookie", principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return role == "Staff"
                ? RedirectToAction("Dashboard", "Staff")
                : RedirectToAction("Dashboard");
        }

        // ── POST /Admin/Logout ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminCookie");
            return RedirectToAction("Login");
        }

        // ── GET /Admin/Dashboard ──
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded)
                return RedirectToAction("Login");

            // This dashboard shows money (revenue, logistics orders/payments) —
            // Staff accounts are limited to the aesthetics-only Staff dashboard.
            if (!IsAdmin(result))
                return RedirectToAction("Dashboard", "Staff");

            // Purge permanently — delete records soft-deleted more than 15 days ago
            PurgeOldRecords();

            ViewBag.FixedPickupTimeDisplay = _config["LogisticsSettings:FixedPickupTimeDisplay"];
            ViewBag.VapidPublicKey = _config["Vapid:PublicKey"]?.Trim();

            var currentAdminIdForPush = int.Parse(result.Principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            ViewBag.PushEnabled = _db.AdminPushSubscriptions.Any(s => s.AdminId == currentAdminIdForPush);

            // Stats
            ViewBag.TotalOrders = _db.LogisticsOrders.Count(o => !o.IsDeleted);
            ViewBag.TotalRequests = _db.AestheticRequests.Count(r => !r.IsDeleted);
            ViewBag.PendingOrders = _db.LogisticsOrders.Count(o => !o.IsDeleted && o.OrderStatus == "Confirmed");
            ViewBag.PendingRequests = _db.AestheticRequests.Count(r => !r.IsDeleted && r.Status == "Pending");
            ViewBag.TotalRevenue = _db.Payments.Where(p => p.PaymentStatus == "Paid").Sum(p => (decimal?)p.Amount) ?? 0;

            // Active logistics orders
            ViewBag.Orders = _db.LogisticsOrders
                .Where(o => !o.IsDeleted)
                .Include(o => o.Student)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .OrderByDescending(o => o.OrderedAt)
                .ToList();

            // Active aesthetic requests
            ViewBag.Requests = _db.AestheticRequests
                .Where(r => !r.IsDeleted)
                .Include(r => r.Student)
                .OrderByDescending(r => r.RequestedAt)
                .ToList();

            // Recently completed (soft-deleted, within 15 days)
            ViewBag.RecentlyCompleted = _db.LogisticsOrders
                .Where(o => o.IsDeleted && o.DeletedAt.HasValue
                         && o.DeletedAt.Value > DateTime.UtcNow.AddDays(-15))
                .Include(o => o.Student)
                .OrderByDescending(o => o.DeletedAt)
                .ToList();

            ViewBag.RecentlyCompletedRequests = _db.AestheticRequests
                .Where(r => r.IsDeleted && r.DeletedAt.HasValue
                         && r.DeletedAt.Value > DateTime.UtcNow.AddDays(-15))
                .Include(r => r.Student)
                .OrderByDescending(r => r.DeletedAt)
                .ToList();

            return View();
        }

        // ── POST /Admin/UpdateOrderStatus ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            // Both Admins and Staff can now work the logistics pickup queue —
            // this action never touches or reveals money, so either role may
            // call it. Money stays gated on the Dashboard/UpdateOrderStatus's
            // caller side (Staff's own view never receives amount fields).
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return RedirectToAction("Login");

            var order = _db.LogisticsOrders.Find(orderId);
            if (order == null) return NotFound();

            if (status == "Completed")
            {
                order.IsDeleted = true;
                order.DeletedAt = DateTime.UtcNow;
                order.OrderStatus = "Completed";
            }
            else
            {
                order.OrderStatus = status;
            }

            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Order #{orderId} updated to {status}.";
            return RedirectToRoleHome(result);
        }

        // ── POST /Admin/UpdateRequestStatus ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(int requestId, string status)
        {
            // Both Admins and Staff handle aesthetic bookings, so either role
            // may update a request's status — this action carries no money.
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return RedirectToAction("Login");

            var request = _db.AestheticRequests.Find(requestId);
            if (request == null) return NotFound();

            if (status == "Completed")
            {
                request.IsDeleted = true;
                request.DeletedAt = DateTime.UtcNow;
                request.Status = "Completed";
                request.ResolvedAt = DateTime.UtcNow;
            }
            else
            {
                request.Status = status;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Request #{requestId} updated to {status}.";
            return RedirectToRoleHome(result);
        }

        // ── POST /Admin/SubscribePush ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscribePush([FromBody] PushSubscriptionRequest sub)
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return Unauthorized();
            if (!IsAdmin(result)) return Forbid("AdminCookie");

            if (sub == null || string.IsNullOrEmpty(sub.Endpoint) || sub.Keys == null)
                return BadRequest();

            var adminId = int.Parse(result.Principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var existing = _db.AdminPushSubscriptions.FirstOrDefault(s => s.Endpoint == sub.Endpoint);
            if (existing == null)
            {
                _db.AdminPushSubscriptions.Add(new AdminPushSubscription
                {
                    AdminId = adminId,
                    Endpoint = sub.Endpoint,
                    P256dh = sub.Keys.P256dh,
                    Auth = sub.Keys.Auth,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        // ── POST /Admin/UnsubscribePush ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsubscribePush([FromBody] UnsubscribeRequest req)
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return Unauthorized();
            if (!IsAdmin(result)) return Forbid("AdminCookie");

            if (req == null || string.IsNullOrEmpty(req.Endpoint)) return BadRequest();

            var existing = _db.AdminPushSubscriptions.FirstOrDefault(s => s.Endpoint == req.Endpoint);
            if (existing != null)
            {
                _db.AdminPushSubscriptions.Remove(existing);
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        // ── POST /Admin/SendTestPush ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestPush()
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return Unauthorized();
            if (!IsAdmin(result)) return Forbid("AdminCookie");

            await _push.NotifyAllAdminsAsync(
                "Test notification",
                "If you can see this, push notifications are working on this device.");

            return Ok();
        }

        // ── Helpers ──

        // True only for the "Admin" role — the full-access role that can see
        // money (revenue, logistics orders, payments) and manage accounts.
        // "Staff" accounts are limited to the aesthetics-only Staff dashboard.
        private static bool IsAdmin(AuthenticateResult result)
        {
            var role = result.Principal?.FindFirst(ClaimTypes.Role)?.Value;
            // Treat a missing role claim as Admin (defensive default for any
            // cookie issued before the Staff role existed).
            return string.IsNullOrEmpty(role) || role == "Admin";
        }

        private IActionResult RedirectToRoleHome(AuthenticateResult result)
        {
            return IsAdmin(result)
                ? RedirectToAction("Dashboard")
                : RedirectToAction("Dashboard", "Staff");
        }

        private void PurgeOldRecords()
        {
            var cutoff = DateTime.UtcNow.AddDays(-15);

            var oldOrders = _db.LogisticsOrders
                .Where(o => o.IsDeleted && o.DeletedAt.HasValue && o.DeletedAt.Value < cutoff)
                .ToList();

            if (oldOrders.Any())
            {
                var orderIds = oldOrders.Select(o => o.OrderId).ToList();
                var items = _db.OrderItems.Where(oi => orderIds.Contains(oi.OrderId)).ToList();
                _db.OrderItems.RemoveRange(items);
                _db.LogisticsOrders.RemoveRange(oldOrders);
            }

            var oldRequests = _db.AestheticRequests
                .Where(r => r.IsDeleted && r.DeletedAt.HasValue && r.DeletedAt.Value < cutoff)
                .ToList();

            if (oldRequests.Any())
                _db.AestheticRequests.RemoveRange(oldRequests);

            if (oldOrders.Any() || oldRequests.Any())
                _db.SaveChanges();
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;
            return HashPassword(password) == storedHash;
        }
        // ── GET /Admin/ManageAdmins ──
        [HttpGet]
        public async Task<IActionResult> ManageAdmins()
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return RedirectToAction("Login");
            if (!IsAdmin(result)) return RedirectToAction("Dashboard", "Staff");

            ViewBag.Admins = _db.Admins.OrderBy(a => a.Role).ThenBy(a => a.FullName).ToList();
            return View();
        }

        // ── POST /Admin/AddAdmin ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdmin(string fullName, string email, string password, string role = "Admin")
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return RedirectToAction("Login");
            if (!IsAdmin(result)) return RedirectToAction("Dashboard", "Staff");

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["AdminError"] = "All fields are required.";
                return RedirectToAction("ManageAdmins");
            }

            if (_db.Admins.Any(a => a.Email == email.Trim().ToLower()))
            {
                TempData["AdminError"] = "An account with this email already exists.";
                return RedirectToAction("ManageAdmins");
            }

            if (password.Length < 8)
            {
                TempData["AdminError"] = "Password must be at least 8 characters.";
                return RedirectToAction("ManageAdmins");
            }

            // Only "Admin" or "Staff" are valid roles — anything else falls back to Staff
            // (the lower-privilege option) rather than silently granting full access.
            var safeRole = role == "Admin" ? "Admin" : "Staff";

            var admin = new Admin
            {
                FullName = fullName.Trim(),
                Email = email.Trim().ToLower(),
                PasswordHash = HashPassword(password),
                Role = safeRole,
                CreatedAt = DateTime.UtcNow
            };

            _db.Admins.Add(admin);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{safeRole} account created for {fullName}.";
            return RedirectToAction("ManageAdmins");
        }

        // ── POST /Admin/RemoveAdmin ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(int adminId)
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return RedirectToAction("Login");
            if (!IsAdmin(result)) return RedirectToAction("Dashboard", "Staff");

            // Prevent deleting yourself
            var currentAdminId = int.Parse(result.Principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            if (adminId == currentAdminId)
            {
                TempData["AdminError"] = "You cannot remove your own account.";
                return RedirectToAction("ManageAdmins");
            }

            var admin = _db.Admins.Find(adminId);
            if (admin == null)
                return RedirectToAction("ManageAdmins");

            // Must always keep at least one full Admin account (Staff accounts don't count).
            if (admin.Role != "Staff" && _db.Admins.Count(a => a.Role != "Staff") <= 1)
            {
                TempData["AdminError"] = "Cannot remove the last admin account.";
                return RedirectToAction("ManageAdmins");
            }

            _db.Admins.Remove(admin);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{(admin.Role == "Staff" ? "Staff" : "Admin")} account for {admin.FullName} has been removed.";

            return RedirectToAction("ManageAdmins");
        }


    }

    // ── Push subscription payload shapes (outside the controller, inside the namespace) ──

    public class PushSubscriptionRequest
    {
        public string Endpoint { get; set; } = "";
        public PushSubscriptionKeys? Keys { get; set; }
    }

    public class PushSubscriptionKeys
    {
        public string P256dh { get; set; } = "";
        public string Auth { get; set; } = "";
    }

    public class UnsubscribeRequest
    {
        public string Endpoint { get; set; } = "";
    }
}
