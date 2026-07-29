namespace HostelSite.ViewModels
{
    // ── PAYSTACK VERIFY (received from inline JS callback) ──
    public class PaystackVerifyRequest
    {
        public string Reference { get; set; } = string.Empty;
        public string? OrderData { get; set; }  // JSON string of the order
    }

    // ── PAYSTACK VERIFY RESPONSE (sent back to JS) ──
    public class PaystackVerifyResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? OrderId { get; set; }
    }

    // ── PAYSTACK WEBHOOK PAYLOAD ──
    public class PaystackWebhookPayload
    {
        public string Event { get; set; } = string.Empty;
        public PaystackWebhookData? Data { get; set; }
    }

    public class PaystackWebhookData
    {
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long Amount { get; set; }          // in pesewas
        public string Currency { get; set; } = string.Empty;
        public PaystackWebhookCustomer? Customer { get; set; }
    }

    public class PaystackWebhookCustomer
    {
        public string Email { get; set; } = string.Empty;
    }
}
