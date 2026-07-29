using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HostelSite.Models.Data;
using HostelSite.ViewModels;

namespace HostelSite.Controllers
{
    public class AccountController : Controller
    {
        private readonly HostelDbContext _db;

        public AccountController(HostelDbContext db)
        {
            _db = db;
        }

        // GET /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // POST /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => x.Key + ": " + string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage)))
                    .ToList();
                ViewData["RegisterError"] = "Validation failed: " + string.Join(" | ", errors);
                return View(model);
            }

            if (_db.Students.Any(s => s.Email == model.Email.Trim().ToLower()))
            {
                ViewData["RegisterError"] = "An account with this email already exists.";
                return View(model);
            }

            try
            {
                var student = new Student
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    Phone = model.Phone?.Trim(),
                    PasswordHash = HashPassword(model.Password),
                    StudentNumber = "STU" + DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _db.Students.Add(student);
                await _db.SaveChangesAsync();

                await SignInStudent(student);

                TempData["Success"] = $"Welcome to Nest, {student.FirstName}! Your account has been created.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewData["RegisterError"] = "Could not create account: " + (ex.InnerException?.Message ?? ex.Message);
                return View(model);
            }
        }

        // GET /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var student = _db.Students
                .FirstOrDefault(s => s.Email == model.Email.Trim().ToLower());

            if (student == null || !VerifyPassword(model.Password, student.PasswordHash))
            {
                ViewData["LoginError"] = "Incorrect email or password. Please try again.";
                return View(model);
            }

            await SignInStudent(student);

            TempData["Success"] = $"Welcome back, {student.FirstName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        // POST /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Info"] = "You've been logged out.";
            return RedirectToAction("Index", "Home");
        }

        // GET /Account/Profile
        [HttpGet]
        public IActionResult Profile()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login");

            var student = _db.Students.Find(GetStudentId());
            if (student == null) return RedirectToAction("Login");

            return View(student);
        }

        // ── Helpers ──

        private async Task SignInStudent(Student student)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, student.StudentId.ToString()),
                new Claim(ClaimTypes.Name,           student.FirstName),
                new Claim(ClaimTypes.Email,          student.Email),
                new Claim("FullName", $"{student.FirstName} {student.LastName}")
            };

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc   = DateTimeOffset.UtcNow.AddDays(30)
                });
        }

        private int GetStudentId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
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
    }
}
