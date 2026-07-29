using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HostelSite.Models.Data;
using HostelSite.ViewModels;

namespace HostelSite.Controllers
{
    public class LogisticsController : Controller
    {
        private readonly HostelDbContext _db;
        private readonly IConfiguration _config;

        public LogisticsController(HostelDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // GET /Logistics
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["PaystackKey"] = _config["Paystack:PublicKey"];
            var items = _db.LogisticsItems
                .Where(i => i.IsActive)
                .OrderBy(i => i.Category)
                .ThenBy(i => i.ItemName)
                .Select(i => new LogisticsItemViewModel
                {
                    Id          = i.ItemId,
                    Name        = i.ItemName,
                    Description = i.Description,
                    Price       = i.Price,
                    Category    = i.Category,
                    Icon        = null      // no Icon column in DB — handled by JS in the view
                })
                .ToList();

            ViewBag.Items = items;
            return View();
        }
        // GET /Logistics/Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Checkout", "Logistics") });

            var key = _config["Paystack:PublicKey"];
            ViewData["PaystackKey"] = key;
            return View();
        }

        // GET /Logistics/MyOrders
        [HttpGet]
        public IActionResult MyOrders()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("MyOrders", "Logistics") });

            int studentId = GetStudentId();

            var orders = _db.LogisticsOrders
                .Where(o => o.StudentId == studentId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Item)
                .OrderByDescending(o => o.OrderedAt)
                .ToList()
                .Select(o => new LogisticsOrderSummaryViewModel
                {
                    Id          = o.OrderId,
                    TotalAmount = o.TotalAmount,
                    Status      = o.OrderStatus,
                    CreatedAt   = o.OrderedAt,
                    PickupDate = o.PickupDate.HasValue ? o.PickupDate.Value.ToDateTime(TimeOnly.MinValue) : null,     // no PickupDate column — stored in DeliveryNotes
                    ReturnDate = o.ReturnDate.HasValue ? o.ReturnDate.Value.ToDateTime(TimeOnly.MinValue) : null,     // same — parsed below
                    OrderItems  = o.OrderItems.Select(oi => new OrderItemDetailViewModel
                    {
                        ItemName  = oi.Item?.ItemName ?? "",
                        Icon      = null,
                        Quantity  = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                })
                .ToList();

            ViewBag.Orders = orders;
            return View();
        }

        // ── Helper ──
        private int GetStudentId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
