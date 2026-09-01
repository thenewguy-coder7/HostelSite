using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HostelSite.Models.Data;
using HostelSite.Services;

namespace HostelSite.Controllers
{
    // Staff-facing view of aesthetics bookings only. Deliberately has no access
    // to logistics orders, payments, or revenue — that stays behind
    // AdminController's Role == "Admin" check. Staff and Admin accounts share
    // the same login (AdminCookie) and the same Admins table; only the Role
    // claim differs.
    public class StaffController : Controller
    {
        private readonly HostelDbContext _db;

        public StaffController(HostelDbContext db)
        {
            _db = db;
        }

        // ── GET /Staff/Dashboard ──
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var result = await HttpContext.AuthenticateAsync("AdminCookie");
            if (!result.Succeeded)
                return RedirectToAction("Login", "Admin");

            ViewBag.StaffName = result.Principal!.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

            // Active aesthetic requests — no money/payment data touched anywhere here.
            ViewBag.Requests = _db.AestheticRequests
                .Where(r => !r.IsDeleted)
                .Include(r => r.Student)
                .OrderByDescending(r => r.RequestedAt)
                .ToList();

            ViewBag.RecentlyCompleted = _db.AestheticRequests
                .Where(r => r.IsDeleted && r.DeletedAt.HasValue
                         && r.DeletedAt.Value > DateTime.UtcNow.AddDays(-15))
                .Include(r => r.Student)
                .OrderByDescending(r => r.DeletedAt)
                .ToList();

            // Active logistics orders — same hall-grouped pickup queue as the
            // Admin dashboard, minus every money field (see Dashboard.cshtml,
            // which never projects an amount into the data sent to the page).
            ViewBag.Orders = _db.LogisticsOrders
                .Where(o => !o.IsDeleted)
                .Include(o => o.Student)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .OrderByDescending(o => o.OrderedAt)
                .ToList();

            ViewBag.RecentlyCompletedOrders = _db.LogisticsOrders
                .Where(o => o.IsDeleted && o.DeletedAt.HasValue
                         && o.DeletedAt.Value > DateTime.UtcNow.AddDays(-15))
                .Include(o => o.Student)
                .OrderByDescending(o => o.DeletedAt)
                .ToList();

            return View();
        }
    }
}
