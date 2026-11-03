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
        private readonly IHttpClientFactory _httpClientFactory;

        public LogisticsController(HostelDbContext db, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        // GET /Logistics
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Index", "Logistics") });

            ViewData["PaystackKey"] = _config["Paystack:PublicKey"];
            ViewBag.FixedReturnDate = _config["LogisticsSettings:FixedReturnDateDisplay"];
            ViewBag.FixedPickupTime = _config["LogisticsSettings:FixedPickupTime"];
            ViewBag.FixedPickupTimeDisplay = _config["LogisticsSettings:FixedPickupTimeDisplay"];
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

            ViewBag.FixedReturnDate = _config["LogisticsSettings:FixedReturnDateDisplay"];
            ViewBag.FixedPickupTime = _config["LogisticsSettings:FixedPickupTime"];
            ViewBag.FixedPickupTimeDisplay = _config["LogisticsSettings:FixedPickupTimeDisplay"];

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
                .Where(o => o.StudentId == studentId && !o.IsDeleted)
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

        // POST /Logistics/UploadItemPhoto
        // A student attaches this at booking time — before checkout, before
        // an order even exists — so it can't be tied to an OrderItem row yet.
        // It just uploads to Supabase Storage and hands back a public URL,
        // which the browser carries through sessionStorage/checkout and the
        // server later saves onto the right OrderItem once the order and
        // payment are confirmed. Entirely optional: a failed/skipped upload
        // never blocks adding the item to the cart or checking out.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadItemPhoto(IFormFile? photo)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            if (photo == null || photo.Length == 0)
                return BadRequest(new { error = "No photo received." });

            const long maxBytes = 8 * 1024 * 1024; // 8 MB
            if (photo.Length > maxBytes)
                return BadRequest(new { error = "That photo is too large — please keep it under 8MB." });

            if (string.IsNullOrEmpty(photo.ContentType) || !photo.ContentType.StartsWith("image/"))
                return BadRequest(new { error = "Only image files are allowed." });

            var ext = Path.GetExtension(photo.FileName);
            if (string.IsNullOrEmpty(ext) || ext.Length > 6)
            {
                ext = photo.ContentType switch
                {
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    "image/heic" => ".heic",
                    "image/heif" => ".heif",
                    _ => ".jpg"
                };
            }

            var studentId = GetStudentId();
            var objectPath = $"{studentId}/{Guid.NewGuid():N}{ext}";
            var bucket = _config["Supabase:ItemPhotosBucket"] ?? "item-photos";
            var supabaseUrl = _config["Supabase:Url"]?.TrimEnd('/');

            if (string.IsNullOrEmpty(supabaseUrl) || supabaseUrl.Contains("REPLACE_WITH"))
                return StatusCode(500, new { error = "Photo storage isn't configured yet — contact the admin." });

            try
            {
                var client = _httpClientFactory.CreateClient("SupabaseStorage");
                using var content = new StreamContent(photo.OpenReadStream());
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(photo.ContentType);

                var response = await client.PostAsync($"object/{bucket}/{objectPath}", content);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return StatusCode(502, new { error = "Could not save the photo. Please try again." +
                        (System.Diagnostics.Debugger.IsAttached ? " (" + err + ")" : "") });
                }

                var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{objectPath}";
                return Json(new { url = publicUrl });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Upload failed. Please check your connection and try again." });
            }
        }

        // ── Helper ──
        private int GetStudentId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
