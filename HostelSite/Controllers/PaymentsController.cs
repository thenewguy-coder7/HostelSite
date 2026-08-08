using HostelSite.Models.Data;
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

        public PaymentsController(HostelDbContext db, IConfiguration config, IHttpClientFactory factory)
        {
            _db = db;
            _config = config;
            _http = factory.CreateClient("Paystack");
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

                // 2. Parse order data
                if (string.IsNullOrEmpty(request.OrderData))
                    return Json(new PaystackVerifyResponse { Success = false, Message = "No order data." });

                int studentId = GetStudentId();
                if (studentId == 0)
                    return Json(new PaystackVerifyResponse { Success = false, Message = "Not logged in." });

                var orderItems = JsonSerializer.Deserialize<OrderDataPayload>(
                    request.OrderData,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (orderItems == null || orderItems.Items == null || !orderItems.Items.Any())
                    return Json(new PaystackVerifyResponse { Success = false, Message = "Order items empty." });

                // 3. Save to DB using execution strategy (required when EnableRetryOnFailure is set)
                var strategy = _db.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync(async () =>
                {
                    using var tx = await _db.Database.BeginTransactionAsync();
                    try
                    {
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
                            PaystackReference = request.Reference,
                            Amount = (decimal)amountPesewas / 100m,
                            Currency = "GHS",
                            PaymentStatus = "Paid",
                            PaidAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        });

                        await _db.SaveChangesAsync();
                        await tx.CommitAsync();

                        return Json(new PaystackVerifyResponse { Success = true, OrderId = order.OrderId });
                    }
                    catch (Exception dbEx)
                    {
                        await tx.RollbackAsync();
                        return Json(new PaystackVerifyResponse
                        {
                            Success = false,
                            Message = "DB error: " + (dbEx.InnerException?.Message ?? dbEx.Message)
                        });
                    }
                });
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

                if (existing != null && existing.PaymentStatus != "Paid")
                {
                    existing.PaymentStatus = "Paid";
                    existing.PaidAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }

            return Ok();
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
