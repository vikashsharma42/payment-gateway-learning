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
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class PaymentService : IPaymentService
    {
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

        public async Task<Bill> GetOrCreateBillAsync(string reservationCode, string phone, CancellationToken ct = default)
        {
            var code = (reservationCode ?? string.Empty).Trim().ToUpperInvariant();

            var reservation = await _db.Reservations.AsNoTracking()
                .Include(r => r.FoodOrders)
                .ThenInclude(o => o.Items)
                .FirstOrDefaultAsync(r => r.Code == code, ct);

            // Same message for an unknown code and a wrong phone, so the code alone reveals nothing
            if (reservation is null || !reservation.MatchesPhone(phone))
                throw new DomainException("We could not find a booking with those details.");

            var bills = await _db.Bills
                .Where(b => b.ReservationId == reservation.Id && b.Status != PaymentStatus.Refunded)
                .ToListAsync(ct);

            if (bills.Any(b => b.Status == PaymentStatus.Paid))
                throw new DomainException("This booking has already been paid.");

            var fresh = new Bill(reservation.Id, reservation.RestaurantId, reservation.FoodOrders);

            // Reuse an unpaid bill for the same amount, so repeated clicks do not pile up bills
            var existing = bills.FirstOrDefault(b => b.Amount == fresh.Amount);
            if (existing is not null)
                return existing;

            _db.Bills.Add(fresh);
            await _db.SaveChangesAsync(ct);

            return fresh;
        }

        public async Task<PaymentOrderResult> CreateOrderAsync(
            long billId, string customerEmail, string customerPhone, string returnUrl,
            string idempotencyKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 64)
                throw new DomainException("A valid idempotency key is required.");

            var key = idempotencyKey.Trim();

            // Same key means the same payment: retries, double clicks and browser "back" all land here
            var existing = await _db.Payments.AsNoTracking()
                .FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);

            if (existing is not null)
            {
                if (existing.BillId != billId)
                    throw new DomainException("This idempotency key was already used for another bill.");

                return ToResult(existing);
            }

            // The amount always comes from the bill in the database, never from the client
            var bill = await _db.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == billId, ct);
            if (bill is null)
                return null;

            if (bill.Status != PaymentStatus.Pending)
                throw new DomainException("This bill has already been settled.");

            var session = await _gateway.CreateOrderAsync(
                bill.Amount,
                $"cust_{Guid.NewGuid():N}",
                customerEmail,
                customerPhone,
                returnUrl,
                ct);

            var order = new Payment(
                bill.Id, bill.Amount, session.OrderId, session.PaymentSessionId, key, customerEmail);
            _db.Payments.Add(order);

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
                var winner = await _db.Payments.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);

                if (winner is null)
                    throw;

                return ToResult(winner);
            }

            _logger.LogInformation(
                "Order {GatewayOrderId} created for bill {BillId} (amount {Amount})",
                order.GatewayOrderId, billId, order.Amount);

            return ToResult(order);
        }

        public async Task<Payment> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var order = await _db.Payments.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is null)
                return null;

            // Already confirmed: no need to call Cashfree again
            if (order.Status == PaymentStatus.Paid || order.Status == PaymentStatus.Refunded)
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

            var order = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
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
            var order = await _db.Payments.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is null || !order.MarkAsPaid())
                return false;

            // The bill follows its payment. Both changes are saved together.
            var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == order.BillId, ct);
            bill?.MarkAsPaid();

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Order {GatewayOrderId} marked paid", gatewayOrderId);

            await SendEmailSafelyAsync(
                new PaymentReceivedEmailModel(order.CustomerEmail, order.GatewayOrderId, order.Amount), ct);

            return true;
        }

        public async Task<Payment> RefundAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var order = await _db.Payments.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is null)
                return null;

            if (order.Status == PaymentStatus.Refunded)
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
                    var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == order.BillId, ct);
                    bill?.MarkAsRefunded();

                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("Order {GatewayOrderId} refunded (refund {RefundId})", gatewayOrderId, refundId);

                    await SendEmailSafelyAsync(
                        new RefundIssuedEmailModel(order.CustomerEmail, order.GatewayOrderId, order.Amount), ct);
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

        private static PaymentOrderResult ToResult(Payment order) =>
            new PaymentOrderResult(order.GatewayOrderId, order.PaymentSessionId, order.Amount, "INR");
    }
}
