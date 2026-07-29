using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using HostelSite.Models;
using HostelSite.Models.Data;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──
builder.Services.AddControllersWithViews();

// ── DATABASE ──
builder.Services.AddDbContext<HostelDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// ── COOKIE AUTHENTICATION ──
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Account/Login";
        options.LogoutPath       = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan   = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly  = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite  = SameSiteMode.Strict;
    });

// ── AUTHORIZATION ──
builder.Services.AddAuthorization();

// ── HTTP CLIENT for Paystack API calls ──
builder.Services.AddHttpClient("Paystack", client =>
{
    client.BaseAddress = new Uri("https://api.paystack.co/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── BUILD APP ──
var app = builder.Build();

// ── PIPELINE ──
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Order matters: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

// ── ROUTES ──
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
