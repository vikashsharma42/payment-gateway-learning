using Microsoft.Extensions.Options;
using ProductShop.Payments.Configuration;
using ProductShop.Payments.Gateway;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Payments.Cashfree
{
    public class CashfreeGateway : IPaymentGateway
    {
        private readonly CashfreeOptions _options;
        private readonly HttpClient _http;

        public CashfreeGateway(IOptions<CashfreeOptions> options, HttpClient http)
        {
            _options = options.Value;
            _http = http;
        }

        public async Task<PaymentOrderResult> CreateOrderAsync(
            decimal amount, string customerId, string customerEmail, string customerPhone,
            string returnUrl, CancellationToken ct = default)
        {
            var orderId = $"order_{DateTimeOffset.UtcNow.Ticks}";

            var body = new
            {
                order_id = orderId,
                order_amount = amount,
                order_currency = "INR",
                customer_details = new
                {
                    customer_id = customerId,
                    customer_email = customerEmail,
                    customer_phone = customerPhone
                },
                order_meta = new { return_url = returnUrl + "?order_id={order_id}" }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/orders")
            {
                Content = JsonContent.Create(body)
            };
            AddHeaders(req);

            using var res = await _http.SendAsync(req, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Cashfree create order failed: {raw}");

            using var doc = JsonDocument.Parse(raw);
            var sessionId = doc.RootElement.GetProperty("payment_session_id").GetString()!;

            return new PaymentOrderResult(orderId, sessionId, amount, "INR");
        }

        public async Task<PaymentStatusResult> GetOrderStatusAsync(string orderId, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl}/orders/{orderId}");
            AddHeaders(req);

            using var res = await _http.SendAsync(req, ct);
            var raw = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Cashfree get order failed: {raw}");

            using var doc = JsonDocument.Parse(raw);
            var status = doc.RootElement.GetProperty("order_status").GetString() ?? "UNKNOWN";

            return new PaymentStatusResult(orderId, status);
        }

        // Cashfree signature = base64(HMAC_SHA256(timestamp + rawBody, secretKey))
        public bool VerifyWebhookSignature(string rawBody, string timestamp, string signature)
        {
            var data = Encoding.UTF8.GetBytes(timestamp + rawBody);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
            var computed = Convert.ToBase64String(hmac.ComputeHash(data));

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(signature ?? string.Empty));
        }

        private void AddHeaders(HttpRequestMessage req)
        {
            req.Headers.Add("x-client-id", _options.AppId);
            req.Headers.Add("x-client-secret", _options.SecretKey);
            req.Headers.Add("x-api-version", _options.ApiVersion);
        }
    }
}