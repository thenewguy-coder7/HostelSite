using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HostelSite.Models.Data;
using HostelSite.ViewModels;

namespace HostelSite.Controllers
{
    public class AestheticsController : Controller
    {
        private readonly HostelDbContext _db;

        public AestheticsController(HostelDbContext db)
        {
            _db = db;
        }

        // GET /Aesthetics
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Index", "Aesthetics") });

            return View();
        }

        // POST /Aesthetics/SubmitRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(AestheticRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required fields before submitting.";
                return View("Index");
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["Info"] = "Please log in or create an account to submit a request.";
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Index", "Aesthetics") });
            }

            int studentId = GetStudentId();

            // Match aesthetic by ThemeName
            var aesthetic = _db.RoomAesthetics
                .FirstOrDefault(a => a.ThemeName == model.StyleName);

            var request = new AestheticRequest
            {
                StudentId   = studentId,
                AestheticId = null,          // placeholder — no aesthetic selection in this flow
                RoomId      = null,           // placeholder — no room selection in this flow
                Status      = "Pending",
                Notes       = string.IsNullOrWhiteSpace(model.Notes)
                                ? $"Style: {model.StyleName} | Hostel: {model.Hostel} | Room: {model.RoomNumber} | Date: {model.PreferredDate:dd MMM yyyy}"
                                : $"Style: {model.StyleName} | Hostel: {model.Hostel} | Room: {model.RoomNumber} | Date: {model.PreferredDate:dd MMM yyyy} | Notes: {model.Notes}",
                RequestedAt = DateTime.UtcNow
            };

            _db.AestheticRequests.Add(request);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your aesthetic request has been submitted! We'll be in touch shortly.";
            return RedirectToAction("MyRequests");
        }

        // GET /Aesthetics/MyRequests
        [HttpGet]
        public IActionResult MyRequests()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("MyRequests", "Aesthetics") });

            int studentId = GetStudentId();

            // Parse the stored Notes field back into display fields
            var requests = _db.AestheticRequests
                .Where(r => r.StudentId == studentId && !r.IsDeleted)
                .OrderByDescending(r => r.RequestedAt)
                .ToList()
                .Select(r => new AestheticRequestSummaryViewModel
                {
                    Id          = r.RequestId,
                    StyleName   = ParseNote(r.Notes, "Style") ?? r.Aesthetic?.ThemeName ?? "Unknown",
                    Hostel      = ParseNote(r.Notes, "Hostel") ?? "—",
                    RoomNumber  = ParseNote(r.Notes, "Room") ?? "—",
                    Status      = r.Status,
                    SubmittedAt = r.RequestedAt,
                    PreferredDate = TryParseDate(ParseNote(r.Notes, "Date")),
                    Notes       = ParseNote(r.Notes, "Notes")
                })
                .ToList();

            ViewBag.Requests = requests;
            return View();
        }

        // ── Helpers ──

        private int GetStudentId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        // Parses "Key: Value | Key2: Value2" format stored in Notes
        private static string? ParseNote(string? notes, string key)
        {
            if (string.IsNullOrEmpty(notes)) return null;
            var parts = notes.Split('|');
            foreach (var part in parts)
            {
                var kv = part.Trim().Split(':', 2);
                if (kv.Length == 2 && kv[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                    return kv[1].Trim();
            }
            return null;
        }

        private static DateTime? TryParseDate(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return DateTime.TryParse(value, out var dt) ? dt : null;
        }
    }
}
