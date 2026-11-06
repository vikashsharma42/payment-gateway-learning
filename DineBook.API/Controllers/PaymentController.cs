using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DineBook.API.DataObjects.Request;
using DineBook.API.DataObjects.Response;
using DineBook.Application.Configuration;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _payments;
        private readonly SiteOptions _site;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService payments, IOptions<SiteOptions> site, ILogger<PaymentController> logger)
        {
            _payments = payments;
            _site = site.Value;
            _logger = logger;
        }

        // Rejects webhooks whose signed timestamp is older than 15 minutes (replay protection).
        // Cashfree retries can arrive later than this; those orders are reconciled by PendingOrderReaper.
        private static bool IsFreshTimestamp(string timestamp)
        {
            if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
                return false;

            try
            {
                // Accept epoch seconds or milliseconds
                var sent = value > 100_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(value)
                    : DateTimeOffset.FromUnixTimeSeconds(value);

                return Math.Abs((DateTimeOffset.UtcNow - sent).TotalMinutes) <= 15;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        [HttpPost("create-order")]
        [RequestSizeLimit(4 * 1024)]
        [EnableRateLimiting("create-order")]
        public async Task<ActionResult<CreateOrderResponse>> CreateOrderAsync(
            [FromBody] CreateOrderRequest request, CancellationToken ct)
        {
            // Built from configuration, never from the Host header
            var returnUrl = $"{_site.PublicBaseUrl.TrimEnd('/')}/Payment/Result";

            var session = await _payments.CreateOrderAsync(
                request.ProductId, request.CustomerEmail, request.CustomerPhone,
                returnUrl, request.IdempotencyKey, ct);

            if (session is null)
                return NotFound(new { error = "Product not found" });

            return Ok(new CreateOrderResponse
            {
                OrderId = session.OrderId,
                PaymentSessionId = session.PaymentSessionId,
                Amount = session.Amount,
                Currency = session.Currency
            });
        }

        [HttpGet("status/{orderId}")]
        [EnableRateLimiting("status")]
        public async Task<ActionResult<OrderStatusResponse>> StatusAsync(string orderId, CancellationToken ct)
        {
            var order = await _payments.GetOrderStatusAsync(orderId, ct);
            if (order is null)
                return NotFound();

            return Ok(new OrderStatusResponse
            {
                OrderId = order.GatewayOrderId,
                Status = order.Status.ToString(),
                IsPaid = order.Status == OrderStatus.Paid
            });
        }

        // Called by Cashfree. The signature is verified on the raw body before anything is trusted.
        [HttpPost("webhook")]
        [RequestSizeLimit(64 * 1024)]
        public async Task<IActionResult> WebhookAsync(CancellationToken ct)
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            var timestamp = Request.Headers["x-webhook-timestamp"].ToString();
            var signature = Request.Headers["x-webhook-signature"].ToString();

            if (!IsFreshTimestamp(timestamp))
            {
                _logger.LogWarning("Rejected Cashfree webhook: stale or invalid timestamp");
                return BadRequest("Stale or invalid timestamp");
            }

            try
            {
                var valid = await _payments.HandleWebhookAsync(body, timestamp, signature, ct);

                if (!valid)
                {
                    _logger.LogWarning("Rejected Cashfree webhook: invalid signature");
                    return BadRequest("Invalid signature");
                }

                _logger.LogInformation("Cashfree webhook processed");
                return Ok(new { received = true });
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                // Signed but malformed: a bad payload, not a server error
                _logger.LogWarning(ex, "Rejected Cashfree webhook: malformed payload");
                return BadRequest("Malformed payload");
            }
        }
    }
}
