using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductShop.Application.Services.Interface;
using ProductShop.Domain.Entities;
using ProductShop.Domain.Enum;
using ProductShop.Domain.Exceptions;
using ProductShop.Email;
using ProductShop.Email.EmailModels;
using ProductShop.Infrastructure.Database;
using ProductShop.Payments.Gateway;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Application.Services
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentGateway _gateway;
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            ApplicationDbContext db,
            IPaymentGateway gateway,
            IEmailService emailService,
            ILogger<PaymentService> logger)
        {
            _db = db;
            _gateway = gateway;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PaymentOrderResult> CreateOrderAsync(
            int productId, string customerEmail, string customerPhone, string returnUrl,
            string idempotencyKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 64)
                throw new DomainException("A valid idempotency key is required.");

            var key = idempotencyKey.Trim();

            // Same key means the same order: retries, double clicks and browser "back" all land here
            var existing = await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);

            if (existing is not null)
            {
                if (existing.ProductId != productId)
                    throw new DomainException("This idempotency key was already used for another product.");

                return ToResult(existing);
            }

            // Price always comes from the database, never from the client
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
            if (product is null)
                return null;

            var session = await _gateway.CreateOrderAsync(
                product.Price,
                $"cust_{Guid.NewGuid():N}",
                customerEmail,
                customerPhone,
                returnUrl,
                ct);

            var order = new Orders(
                product.Id, product.Price, session.OrderId, session.PaymentSessionId, key, customerEmail);
            _db.Orders.Add(order);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // A parallel request with the same key saved first. Return that order.
                // The Cashfree order created here stays unused and expires on its own.
                _logger.LogWarning(ex, "Duplicate idempotency key {IdempotencyKey}; returning the existing order", key);

                _db.ChangeTracker.Clear();
                var winner = await _db.Orders.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);

                if (winner is null)
                    throw;

                return ToResult(winner);
            }

            _logger.LogInformation(
                "Order {GatewayOrderId} created for product {ProductId} (amount {Amount})",
                order.GatewayOrderId, productId, order.Amount);

            return ToResult(order);
        }

        public async Task<Orders> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is null)
                return null;

            // Already confirmed: no need to call Cashfree again
            if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Refunded)
                return order;

            try
            {
                var remote = await _gateway.GetOrderStatusAsync(gatewayOrderId, ct);
                if (remote.IsPaid)
                    await MarkPaidAsync(gatewayOrderId, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                // Cashfree is unreachable or returned an error. Show the stored status;
                // the webhook or the reaper will still update it later.
                _logger.LogWarning(ex, "Status check failed for order {GatewayOrderId}", gatewayOrderId);
            }

            return order;
        }

        public async Task<bool> HandleWebhookAsync(string rawBody, string timestamp, string signature, CancellationToken ct = default)
        {
            if (!_gateway.VerifyWebhookSignature(rawBody, timestamp, signature))
                return false;

            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var type) || type.GetString() != "PAYMENT_SUCCESS_WEBHOOK")
                return true; // valid, but an event we do not act on

            var orderJson = root.GetProperty("data").GetProperty("order");
            var gatewayOrderId = orderJson.GetProperty("order_id").GetString();
            var paidAmount = orderJson.GetProperty("order_amount").GetDecimal();

            if (string.IsNullOrWhiteSpace(gatewayOrderId))
                return true;

            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is null)
            {
                _logger.LogWarning("Webhook for unknown order {GatewayOrderId}", gatewayOrderId);
                return true;
            }

            // The signature proves Cashfree sent it, not that the customer paid the right amount
            if (paidAmount != order.Amount)
            {
                _logger.LogError(
                    "Amount mismatch for order {GatewayOrderId}: expected {Expected}, webhook reported {Paid}",
                    gatewayOrderId, order.Amount, paidAmount);
                return true; // signed, but never mark paid
            }

            try
            {
                await MarkPaidAsync(gatewayOrderId, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Webhook could not change order {GatewayOrderId}", gatewayOrderId);
            }

            return true;
        }

        public async Task<bool> MarkPaidAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is null || !order.MarkAsPaid())
                return false;

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Order {GatewayOrderId} marked paid", gatewayOrderId);

            await SendEmailSafelyAsync("payment-received", order.GatewayOrderId,
                () => _emailService.SendAsync(
                    new PaymentReceivedEmailModel(order.CustomerEmail, order.GatewayOrderId, order.Amount), ct), ct);

            return true;
        }

        public async Task<Orders> RefundAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is null)
                return null;

            if (order.Status == OrderStatus.Refunded)
                return order;

            // The refund id is stored before calling Cashfree, so a retry reuses the same id.
            // Cashfree rejects a duplicate id, which means a refund can never be issued twice.
            if (order.RefundId is null)
            {
                order.RecordRefundRequest($"rfnd_{Guid.NewGuid():N}"); // throws unless the order is paid
                await _db.SaveChangesAsync(ct);
            }

            var refundId = order.RefundId;

            RefundResult result;
            try
            {
                result = await _gateway.CreateRefundAsync(
                    order.GatewayOrderId, refundId, order.Amount, "Refund issued by merchant", ct);
            }
            catch (InvalidOperationException)
            {
                // Cashfree already has this refund id from an earlier attempt. Read its status instead.
                result = await _gateway.GetRefundStatusAsync(order.GatewayOrderId, refundId, ct);
            }

            if (result.IsSuccess)
            {
                if (order.MarkAsRefunded())
                {
                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("Order {GatewayOrderId} refunded (refund {RefundId})", gatewayOrderId, refundId);

                    await SendEmailSafelyAsync("refund-issued", order.GatewayOrderId,
                        () => _emailService.SendAsync(
                            new RefundIssuedEmailModel(order.CustomerEmail, order.GatewayOrderId, order.Amount), ct), ct);
                }

                return order;
            }

            if (result.Status == "CANCELLED")
            {
                // Cashfree cancelled this attempt, so the next request may use a new refund id
                order.ClearRefundRequest();
                await _db.SaveChangesAsync(ct);
            }

            // PENDING: the order stays Paid with its refund id. Call RefundAsync again later to reconcile.
            _logger.LogInformation("Refund {RefundId} for order {GatewayOrderId} is {Status}", refundId, gatewayOrderId, result.Status);
            return order;
        }

        private async Task SendEmailSafelyAsync(
            string kind, string gatewayOrderId, Func<Task> send, CancellationToken ct)
        {
            try
            {
                // Rendering and sending both run inside the try: an email failure must never undo a payment or refund
                await send();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Email '{EmailKind}' for order {GatewayOrderId} could not be sent", kind, gatewayOrderId);
            }
        }

        private static PaymentOrderResult ToResult(Orders order) =>
            new PaymentOrderResult(order.GatewayOrderId, order.PaymentSessionId, order.Amount, "INR");
    }
}
