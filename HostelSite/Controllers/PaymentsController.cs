using HostelSite.Models.Data;
using HostelSite.Services;
using HostelSite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HostelSite.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly HostelDbContext _db;
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        private readonly PushNotificationService _push;

        public PaymentsController(HostelDbContext db, IConfiguration config, IHttpClientFactory factory, PushNotificationService push)
        {
            _db = db;
            _config = config;
            _http = factory.CreateClient("Paystack");
            _push = push;
        }

        // GET /Payments/Success
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        // POST /Payments/Verify
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify([FromBody] PaystackVerifyRequest request)
        {
            if (string.IsNullOrEmpty(request.Reference))
                return Json(new PaystackVerifyResponse { Success = false, Message = "No reference provided." });

            try
            {
                // 1. Verify with Paystack API
                var secretKey = _config["Paystack:SecretKey"];

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

                var apiResponse = await httpClient.GetAsync(
                    $"https://api.paystack.co/transaction/verify/{request.Reference}");

                var json = await apiResponse.Content.ReadAsStringAsync();

                if (!apiResponse.IsSuccessStatusCode)
                    return Json(new PaystackVerifyResponse { Success = false, Message = "Paystack API error: " + json });

                var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                var status = data.GetProperty("status").GetString();
                var amountPesewas = data.GetProperty("amount").GetInt64();

                if (status != "success")
                    return Json(new PaystackVerifyResponse { Success = false, Message = "Payment status: " + status });

                // 2. Create the order — or, if the webhook already beat this
                // callback to it, do nothing (see the shared helper's
                // idempotency check).
                var (success, message, orderId) = await CreateOrderFromChargeAsync(
                    request.Reference, amountPesewas, request.OrderData, GetStudentId());

                return Json(new PaystackVerifyResponse { Success = success, Message = message, OrderId = orderId });
            }
            catch (Exception ex)
            {
                return Json(new PaystackVerifyResponse
                {
                    Success = false,
                    Message = "Verify error: " + ex.Message
                });
            }
        }

        // POST /Payments/Webhook
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            var secret = _config["Paystack:SecretKey"] ?? "";
            var signature = Request.Headers["x-paystack-signature"].ToString();

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            if (!string.Equals(ComputeHmacSha512(body, secret), signature, StringComparison.OrdinalIgnoreCase))
                return Unauthorized();

            PaystackWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<PaystackWebhookPayload>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return Ok(); }

            if (payload?.Event == "charge.success" && payload.Data != null)
            {
                var existing = _db.Payments
                    .FirstOrDefault(p => p.PaystackReference == payload.Data.Reference);

                if (existing != null)
                {
                    if (existing.PaymentStatus != "Paid")
                    {
                        existing.PaymentStatus = "Paid";
                        existing.PaidAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    // Safety net: no order was ever created for this charge —
                    // most likely the student's phone/browser lost the
                    // connection or the app was closed right after paying,
                    // before it could call /Payments/Verify itself. Paystack
                    // still says the charge succeeded, so build the order
                    // from here using the cart JSON we stashed in the
                    // transaction's metadata at checkout, rather than leaving
                    // the student charged with no booking to show for it.
                    var email = payload.Data.Customer?.Email;
                    var student = !string.IsNullOrEmpty(email)
                        ? _db.Students.FirstOrDefault(s => s.Email == email)
                        : null;

                    if (student != null)
                    {
                        await CreateOrderFromChargeAsync(
                            payload.Data.Reference,
                            payload.Data.Amount,
                            payload.Data.Metadata?.OrderData,
                            student.StudentId);
                    }
                    // If we can't match a student by email, there's nothing
                    // safe to auto-create — it needs a manual look-up against
                    // the Paystack dashboard by reference.
                }
            }

            return Ok();
        }

        // Creates the LogisticsOrder + OrderItems + Payment for a successful
        // Paystack charge. Both /Payments/Verify (the normal path, right
        // after the student pays) and /Payments/Webhook (the fallback, in
        // case that browser callback never arrives) call this, so it's
        // idempotent on PaystackReference — whichever of the two gets here
        // first wins, and the other becomes a harmless no-op.
        private async Task<(bool Success, string? Message, int? OrderId)> CreateOrderFromChargeAsync(
            string reference, long amountPesewas, string? orderDataJson, int studentId)
        {
            if (_db.Payments.Any(p => p.PaystackReference == reference))
                return (true, "Order already recorded for this payment.", null);

            if (studentId == 0)
                return (false, "Not logged in.", null);

            if (string.IsNullOrEmpty(orderDataJson))
                return (false, "No order data.", null);

            OrderDataPayload? orderItems;
            try
            {
                orderItems = JsonSerializer.Deserialize<OrderDataPayload>(
                    orderDataJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return (false, "Could not parse order data: " + ex.Message, null);
            }

            if (orderItems == null || orderItems.Items == null || !orderItems.Items.Any())
                return (false, "Order items empty.", null);

            // Save to DB using execution strategy (required when EnableRetryOnFailure is set)
            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    // Re-check inside the transaction — Verify() and the
                    // webhook can arrive within milliseconds of each other,
                    // and the outer check above doesn't close that race.
                    if (_db.Payments.Any(p => p.PaystackReference == reference))
                        return (true, "Order already recorded for this payment.", (int?)null);

                    var deliveryNotes = $"Pickup: {orderItems.Pickup} | Return: {orderItems.ReturnDate}";

                    var order = new LogisticsOrder
                    {
                        StudentId = studentId,
                        TotalAmount = (decimal)amountPesewas / 100m,
                        OrderStatus = "Confirmed",
                        DeliveryNotes = deliveryNotes,
                        PickupDate = !string.IsNullOrEmpty(orderItems.Pickup) ? DateOnly.FromDateTime(DateTime.Parse(orderItems.Pickup)) : null,
                        PickupTime = !string.IsNullOrEmpty(orderItems.PickupTime) ? TimeOnly.Parse(orderItems.PickupTime) : null,
                        PreviousHostel = orderItems.PreviousHostel,
                        NewHostel = orderItems.NewHostel,
                        RoomNumber = orderItems.RoomNumber,
                        Phone = orderItems.Phone,
                        ReturnDate = !string.IsNullOrEmpty(orderItems.ReturnDate) ? DateOnly.Parse(orderItems.ReturnDate, System.Globalization.CultureInfo.GetCultureInfo("en-GB")) : null,
                        OrderedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _db.LogisticsOrders.Add(order);
                    await _db.SaveChangesAsync();

                    foreach (var line in orderItems.Items)
                    {
                        _db.OrderItems.Add(new OrderItem
                        {
                            OrderId = order.OrderId,
                            ItemId = line.ItemId,
                            Quantity = line.Quantity,
                            UnitPrice = (decimal)line.Price
                        });
                    }

                    _db.Payments.Add(new Payment
                    {
                        BookingId = null,
                        PaystackReference = reference,
                        Amount = (decimal)amountPesewas / 100m,
                        Currency = "GHS",
                        PaymentStatus = "Paid",
                        PaidAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    var student = await _db.Students.FindAsync(studentId);
                    var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "A student";
                    var pickupText = order.PickupDate.HasValue ? order.PickupDate.Value.ToString("dd MMM yyyy") : "an unscheduled date";
                    await _push.NotifyAllAdminsAsync(
                        "New storage booking",
                        $"{studentName} booked storage (Order #{order.OrderId}) — pickup {pickupText}.",
                        "/Admin/Dashboard");

                    return (true, (string?)null, (int?)order.OrderId);
                }
                catch (Exception dbEx)
                {
                    await tx.RollbackAsync();
                    return (false, "DB error: " + (dbEx.InnerException?.Message ?? dbEx.Message), (int?)null);
                }
            });
        }

        // ── Helpers ──
        private int GetStudentId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private static string ComputeHmacSha512(string data, string key)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }

    }  // end of PaymentsController

    // ── Helper payload classes (outside controller, inside namespace) ──

    public class OrderDataPayload
    {
        public List<OrderLineItem> Items { get; set; } = new();
        public decimal Total { get; set; }
        public string? Pickup { get; set; }
        public string? PickupTime { get; set; }
        public string? PreviousHostel { get; set; }
        public string? NewHostel { get; set; }
        public string? RoomNumber { get; set; }
        public string? Phone { get; set; }
        public string? ReturnDate { get; set; }
    }

    public class OrderLineItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public int Quantity { get; set; }
    }

}  // end of namespace
