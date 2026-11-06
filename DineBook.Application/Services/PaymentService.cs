using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Email;
using DineBook.Email.EmailModels;
using DineBook.Infrastructure.Database;
using DineBook.Payments.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class PaymentService : IPaymentService
    {
        // Cashfree payment sessions expire. A pending payment older than this is not reused.
        private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);

        private readonly ApplicationDbContext _db;
        private readonly IPaymentGateway _gateway;
        private readonly IEmailService _email;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            ApplicationDbContext db,
            IPaymentGateway gateway,
            IEmailService email,
            ILogger<PaymentService> logger)
        {
            _db = db;
            _gateway = gateway;
            _email = email;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Orders>> GetPayableOrdersAsync(string reservationCode, string phone, CancellationToken ct = default)
        {
            var code = (reservationCode ?? string.Empty).Trim().ToUpperInvariant();

            var reservation = await _db.Reservations.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Code == code, ct);

            // Same message for an unknown code and a wrong phone, so the code alone reveals nothing
            if (reservation is null || !reservation.MatchesPhone(phone))
                throw new DomainException("We could not find a booking with those details.");

            var orders = await _db.Orders.AsNoTracking()
                .Where(o => o.ReservationId == reservation.Id && o.Status != OrderStatus.Cancelled)
                .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
                .Include(o => o.Payments)
                .OrderBy(o => o.Id)
                .ToListAsync(ct);

            if (orders.Count == 0)
                throw new DomainException("There is nothing to pay yet. Order some food first.");

            return orders;
        }

        public async Task<PaymentOrderResult> CreateOrderAsync(
            long orderId, string customerEmail, string customerPhone, string returnUrl,
            string idempotencyKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 64)
                throw new DomainException("A valid idempotency key is required.");

            var key = idempotencyKey.Trim();

            // Same key means the same payment: retries, double clicks and browser "back" all land here
            var existing = await _db.Payments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);

            if (existing is not null)
            {
                if (existing.OrderId != orderId)
                    throw new DomainException("This idempotency key was already used for another order.");

                return ToResult(existing);
            }

            // The amount always comes from the order in the database, never from the client
            var order = await _db.Orders.AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order is null)
                return null;

            if (order.Status == OrderStatus.Cancelled)
                throw new DomainException("This order was cancelled and cannot be paid.");

            if (order.IsPaid)
                throw new DomainException("This order has already been paid.");

            // Reuse a fresh pending payment for this order, so two checkouts never charge the same order twice
            var pending = order.Payments.FirstOrDefault(p =>
                p.Status == PaymentStatus.Pending && DateTimeOffset.UtcNow - p.CreatedDateTime < SessionLifetime);
            if (pending is not null)
                return ToResult(pending);

            var session = await _gateway.CreateOrderAsync(
                order.Total(),
                $"cust_{Guid.NewGuid():N}",
                customerEmail,
                customerPhone,
                returnUrl,
                ct);

            // The Payment constructor takes its amount from the order again, so both always agree
            var payment = new Payment(
                order, session.OrderId, session.PaymentSessionId, key, customerEmail);
            _db.Payments.Add(payment);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // A parallel request with the same key saved first. Return that payment.
                // The Cashfree order created here stays unused and expires on its own.
                _logger.LogWarning(ex, "Duplicate idempotency key {IdempotencyKey}; returning the existing payment", key);

                _db.ChangeTracker.Clear();
                var winner = await _db.Payments.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);

                if (winner is null)
                    throw;

                return ToResult(winner);
            }

            _logger.LogInformation(
                "Payment {GatewayOrderId} created for order {OrderId} (amount {Amount})",
                payment.GatewayOrderId, orderId, payment.Amount);

            return ToResult(payment);
        }

        public async Task<Payment> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.GatewayOrderId == gatewayOrderId, ct);
            if (payment is null)
                return null;

            // Already confirmed: no need to call Cashfree again
            if (payment.Status == PaymentStatus.Paid || payment.Status == PaymentStatus.Refunded)
                return payment;

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
                _logger.LogWarning(ex, "Status check failed for payment {GatewayOrderId}", gatewayOrderId);
            }

            return payment;
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

            var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.GatewayOrderId == gatewayOrderId, ct);
            if (payment is null)
            {
                _logger.LogWarning("Webhook for unknown payment {GatewayOrderId}", gatewayOrderId);
                return true;
            }

            // The signature proves Cashfree sent it, not that the customer paid the right amount
            if (paidAmount != payment.Amount)
            {
                _logger.LogError(
                    "Amount mismatch for payment {GatewayOrderId}: expected {Expected}, webhook reported {Paid}",
                    gatewayOrderId, payment.Amount, paidAmount);
                return true; // signed, but never mark paid
            }

            try
            {
                await MarkPaidAsync(gatewayOrderId, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Webhook could not change payment {GatewayOrderId}", gatewayOrderId);
            }

            return true;
        }

        public async Task<bool> MarkPaidAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.GatewayOrderId == gatewayOrderId, ct);
            if (payment is null || !payment.MarkAsPaid())
                return false;

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Payment {GatewayOrderId} marked paid", gatewayOrderId);

            await SendEmailSafelyAsync(
                new PaymentReceivedEmailModel(payment.CustomerEmail, payment.GatewayOrderId, payment.Amount), ct);

            return true;
        }

        public async Task<Payment> RefundAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.GatewayOrderId == gatewayOrderId, ct);
            if (payment is null)
                return null;

            if (payment.Status == PaymentStatus.Refunded)
                return payment;

            // The refund id is stored before calling Cashfree, so a retry reuses the same id.
            // Cashfree rejects a duplicate id, which means a refund can never be issued twice.
            if (payment.RefundId is null)
            {
                payment.RecordRefundRequest($"rfnd_{Guid.NewGuid():N}"); // throws unless the payment is paid
                await _db.SaveChangesAsync(ct);
            }

            var refundId = payment.RefundId;

            RefundResult result;
            try
            {
                result = await _gateway.CreateRefundAsync(
                    payment.GatewayOrderId, refundId, payment.Amount, "Refund issued by merchant", ct);
            }
            catch (InvalidOperationException)
            {
                // Cashfree already has this refund id from an earlier attempt. Read its status instead.
                result = await _gateway.GetRefundStatusAsync(payment.GatewayOrderId, refundId, ct);
            }

            if (result.IsSuccess)
            {
                if (payment.MarkAsRefunded())
                {
                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("Payment {GatewayOrderId} refunded (refund {RefundId})", gatewayOrderId, refundId);

                    await SendEmailSafelyAsync(
                        new RefundIssuedEmailModel(payment.CustomerEmail, payment.GatewayOrderId, payment.Amount), ct);
                }

                return payment;
            }

            if (result.Status == "CANCELLED")
            {
                // Cashfree cancelled this attempt, so the next request may use a new refund id
                payment.ClearRefundRequest();
                await _db.SaveChangesAsync(ct);
            }

            // PENDING: the payment stays Paid with its refund id. Call RefundAsync again later to reconcile.
            _logger.LogInformation("Refund {RefundId} for payment {GatewayOrderId} is {Status}", refundId, gatewayOrderId, result.Status);
            return payment;
        }

        private async Task SendEmailSafelyAsync(BaseEmailModel message, CancellationToken ct)
        {
            try
            {
                await _email.SendAsync(message, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed email must never undo a payment or refund that already succeeded
                _logger.LogWarning(ex, "Email '{Subject}' could not be sent", message.Subject);
            }
        }

        private static PaymentOrderResult ToResult(Payment payment) =>
            new PaymentOrderResult(payment.GatewayOrderId, payment.PaymentSessionId, payment.Amount, "INR");
    }
}
