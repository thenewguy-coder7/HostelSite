using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HostelSite.Models.Data;

namespace HostelSite.Controllers
{
    public class AdminController : Controller
    {
        private readonly HostelDbContext _db;
        private readonly IConfiguration _config;

        public AdminController(HostelDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // ── GET /Admin/Login ──
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (result.Succeeded)
                return RedirectToAction("Dashboard");
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name,           admin.FullName),
                new Claim(ClaimTypes.Email,          admin.Email),
                new Claim(ClaimTypes.Role,           "Admin")
            };

            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("AdminCookie", principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return RedirectToAction("Dashboard");
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

            // Purge permanently — delete records soft-deleted more than 15 days ago
            PurgeOldRecords();

            ViewBag.FixedPickupTimeDisplay = _config["LogisticsSettings:FixedPickupTimeDisplay"];
            ViewBag.VapidPublicKey = _config["Vapid:PublicKey"];

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
            return RedirectToAction("Dashboard");
        }

        // ── POST /Admin/UpdateRequestStatus ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(int requestId, string status)
        {
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
            return RedirectToAction("Dashboard");
        }

        // ── POST /Admin/SubscribePush ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscribePush([FromBody] PushSubscriptionRequest sub)
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return Unauthorized();

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

            if (req == null || string.IsNullOrEmpty(req.Endpoint)) return BadRequest();

            var existing = _db.AdminPushSubscriptions.FirstOrDefault(s => s.Endpoint == req.Endpoint);
            if (existing != null)
            {
                _db.AdminPushSubscriptions.Remove(existing);
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        // ── Helpers ──
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

            ViewBag.Admins = _db.Admins.OrderBy(a => a.FullName).ToList();
            return View();
        }

        // ── POST /Admin/AddAdmin ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdmin(string fullName, string email, string password)
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["AdminError"] = "All fields are required.";
                return RedirectToAction("ManageAdmins");
            }

            if (_db.Admins.Any(a => a.Email == email.Trim().ToLower()))
            {
                TempData["AdminError"] = "An admin with this email already exists.";
                return RedirectToAction("ManageAdmins");
            }

            if (password.Length < 8)
            {
                TempData["AdminError"] = "Password must be at least 8 characters.";
                return RedirectToAction("ManageAdmins");
            }

            var admin = new Admin
            {
                FullName = fullName.Trim(),
                Email = email.Trim().ToLower(),
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            _db.Admins.Add(admin);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Admin account created for {fullName}.";
            return RedirectToAction("ManageAdmins");
        }

        // ── POST /Admin/RemoveAdmin ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(int adminId)
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded) return RedirectToAction("Login");

            // Prevent deleting yourself
            var currentAdminId = int.Parse(result.Principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            if (adminId == currentAdminId)
            {
                TempData["AdminError"] = "You cannot remove your own account.";
                return RedirectToAction("ManageAdmins");
            }

            // Must always keep at least one admin
            if (_db.Admins.Count() <= 1)
            {
                TempData["AdminError"] = "Cannot remove the last admin account.";
                return RedirectToAction("ManageAdmins");
            }

            var admin = _db.Admins.Find(adminId);
            if (admin != null)
            {
                _db.Admins.Remove(admin);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Admin account for {admin.FullName} has been removed.";
            }

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
