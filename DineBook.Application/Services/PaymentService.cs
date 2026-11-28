using DineBook.Application.Configuration;
using DineBook.Application.Events;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Domain.Specifications;
using DineBook.Domain.ValueObjects;
using DineBook.Email;
using DineBook.Email.EmailModels;
using DineBook.Infrastructure.Database;
using DineBook.Payments.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        private readonly IDomainEventDispatcher _events;
        private readonly ILogger<PaymentService> _logger;
        private readonly decimal _bookingAdvance;
        private readonly decimal _cancellationFee;

        public PaymentService(
            ApplicationDbContext db,
            IPaymentGateway gateway,
            IEmailService email,
            IDomainEventDispatcher events,
            ILogger<PaymentService> logger,
            IOptions<DiningOptions> dining)
        {
            _db = db;
            _bookingAdvance = dining.Value.BookingAdvance;
            _cancellationFee = dining.Value.CancellationFee;
            _gateway = gateway;
            _email = email;
            _events = events;
            _logger = logger;
        }

        public decimal BookingAdvance => _bookingAdvance;

        public decimal CancellationFee => _cancellationFee;

        // Deposits paid for this booking that are not refunded
        public async Task<decimal> DepositHeldAsync(long reservationId, CancellationToken ct = default) =>
            await _db.Payments.AsNoTracking()
                .Where(p => p.ReservationId == reservationId && p.OrderId == null && p.Status == PaymentStatus.Paid)
                .SumAsync(p => p.Amount.Amount - p.RefundedAmount, ct);

        // Deposit money already used for food: paid, refunded, or pending and still within the session lifetime
        private async Task<decimal> UsedAdvanceAsync(long reservationId, CancellationToken ct)
        {
            var cutoff = DateTimeOffset.UtcNow - SessionLifetime;

            return await _db.Payments.AsNoTracking()
                .Where(p => p.ReservationId == reservationId
                    && p.OrderId != null
                    && (p.Status == PaymentStatus.Paid
                        || p.Status == PaymentStatus.Refunded
                        || (p.Status == PaymentStatus.Pending && p.CreatedDateTime >= cutoff)))
                .SumAsync(p => p.AdvanceApplied, ct);
        }

        // The deposit still available for this order: deposit held less what food already used, capped at the bill
        public async Task<decimal> AdvanceCreditAsync(Orders order, CancellationToken ct = default)
        {
            var bill = order.BillFor(order.Restaurant).Total.Amount;
            var held = await DepositHeldAsync(order.ReservationId, ct);
            var used = await UsedAdvanceAsync(order.ReservationId, ct);

            return Math.Min(Math.Max(0m, held - used), bill);
        }

        public async Task<PaymentOrderResult> CreateDepositAsync(
            string reservationCode, string customerEmail, string customerPhone, string returnUrl,
            string idempotencyKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 64)
                throw new DomainException("A valid idempotency key is required.");

            var key = idempotencyKey.Trim();
            var existing = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);
            if (existing is not null)
                return ToResult(existing);

            var code = (reservationCode ?? string.Empty).Trim().ToUpperInvariant();
            var reservation = await _db.Reservations.AsNoTracking()
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Code == code, ct)
                ?? throw new DomainException("Booking not found.");

            var customer = reservation.Customer
                ?? throw new DomainException("Customer information is not available for this booking.");

            if (ApplicationUser.NormalizeEmail(customerEmail) != customer.Email
                || ApplicationUser.NormalizePhone(customerPhone) != customer.Phone)
                throw new DomainException("Customer details do not match this booking.");

            if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.NoShow)
                throw new DomainException("This booking is no longer active.");

            if (_bookingAdvance <= 0m)
                throw new DomainException("No booking deposit is required.");

            if (await DepositHeldAsync(reservation.Id, ct) > 0m)
                throw new DomainException("The booking deposit is already paid.");

            var now = DateTimeOffset.UtcNow;
            var pending = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p =>
                p.ReservationId == reservation.Id && p.OrderId == null && p.Status == PaymentStatus.Pending
                && p.CreatedDateTime >= now - SessionLifetime, ct);
            if (pending is not null)
                return ToResult(pending);

            var session = await _gateway.CreateOrderAsync(
                _bookingAdvance,
                $"dep_{Guid.NewGuid():N}",
                customerEmail,
                customerPhone,
                returnUrl,
                ct);

            var payment = new Payment(
                reservation.Id,
                Money.Create(_bookingAdvance, Money.DefaultCurrency),
                session.OrderId,
                session.PaymentSessionId,
                key,
                customerEmail);
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Deposit {GatewayOrderId} created for booking {Code} (amount {Amount})",
                payment.GatewayOrderId, code, payment.Amount.Amount);

            return ToResult(payment);
        }

        public async Task<decimal> RefundCancelledBookingAsync(string reservationCode, CancellationToken ct = default)
        {
            var code = (reservationCode ?? string.Empty).Trim().ToUpperInvariant();
            var reservation = await _db.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Code == code, ct);
            if (reservation is null || reservation.Status != ReservationStatus.Cancelled)
                return 0m;

            // Tracked, so the refund id and refunded amount are saved
            var deposit = await _db.Payments.FirstOrDefaultAsync(p =>
                p.ReservationId == reservation.Id && p.OrderId == null && p.Status == PaymentStatus.Paid, ct);
            if (deposit is null)
                return 0m;

            // Once food has been paid from the deposit, it is not refunded
            if (await UsedAdvanceAsync(reservation.Id, ct) > 0m)
                return 0m;

            var refundable = Math.Max(0m, deposit.Amount.Amount - deposit.RefundedAmount - _cancellationFee);
            if (refundable <= 0m)
                return 0m;

            // The refund id is stored before calling the gateway, so a retry reuses it
            if (deposit.RefundId is null)
            {
                deposit.RecordRefundRequest($"rfnd_{Guid.NewGuid():N}");
                await _db.SaveChangesAsync(ct);
            }

            var refundId = deposit.RefundId;
            RefundResult result;
            try
            {
                result = await _gateway.CreateRefundAsync(
                    deposit.GatewayOrderId, refundId, refundable, "Booking cancelled", ct);
            }
            catch (InvalidOperationException)
            {
                // The gateway already has this refund id from an earlier attempt. Read its status instead.
                result = await _gateway.GetRefundStatusAsync(deposit.GatewayOrderId, refundId, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Deposit refund {RefundId} for booking {Code} could not be sent; it is retried later", refundId, code);
                return 0m;
            }

            if (!result.IsSuccess)
            {
                if (result.Status == "CANCELLED")
                {
                    deposit.ClearRefundRequest();
                    await _db.SaveChangesAsync(ct);
                }

                return 0m;
            }

            deposit.RecordPartialRefund(refundable);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Deposit {GatewayOrderId} partly refunded ({Amount}) for booking {Code}",
                deposit.GatewayOrderId, refundable, code);

            await SendEmailSafelyAsync(
                new RefundIssuedEmailModel(deposit.CustomerEmail, deposit.GatewayOrderId, refundable), ct);

            return refundable;
        }

        public async Task<PaymentBookingLinks> GetPaymentLinksAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var payment = await _db.Payments.AsNoTracking()
                .Matching(new PaymentByGatewayOrderIdSpecification(gatewayOrderId))
                .FirstOrDefaultAsync(ct);

            if (payment?.ReservationId is not long reservationId)
                return null;

            var reservation = await _db.Reservations.AsNoTracking()
                .Include(r => r.Restaurant)
                .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

            return reservation is null
                ? null
                : new PaymentBookingLinks(reservation.Code, reservation.Restaurant?.Slug ?? string.Empty);
        }

        public async Task<Orders> GetOrderForCustomerAsync(long orderId, string restaurantSlug, long customerId, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
                .Include(o => o.Payments)
                .Include(o => o.Restaurant)
                .Include(o => o.Reservation)
                .AsSingleQuery()
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            // The order must belong to this restaurant's address and to the signed-in owner of the booking.
            if (order is null
                || !string.Equals(order.Restaurant?.Slug, restaurantSlug, StringComparison.OrdinalIgnoreCase)
                || order.Reservation?.ApplicationUserId != customerId)
                return null;

            return order;
        }

        public async Task<PaymentBookingLinks> GetBookingLinksAsync(long orderId, CancellationToken ct = default)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Reservation)
                .Include(o => o.Restaurant)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order is null)
                return null;

            return new PaymentBookingLinks(order.Reservation?.Code ?? string.Empty, order.Restaurant?.Slug ?? string.Empty);
        }

        public async Task<IReadOnlyList<Orders>> GetPayableOrdersAsync(string reservationCode, string phone, CancellationToken ct = default)
        {
            var code = (reservationCode ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedPhone = ApplicationUser.NormalizePhone(phone);

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(normalizedPhone))
                throw new DomainException("We couldn't find a booking with that code and phone number. Please check them and try again.");

            var reservation = await _db.Reservations
                .AsNoTracking()
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Code == code, ct);

            if (reservation?.Customer is null || reservation.Customer.Phone != normalizedPhone)
                throw new DomainException("We couldn't find a booking with that code and phone number. Please check them and try again.");

            var orders = await _db.Orders.AsNoTracking()
                .Where(o => o.ReservationId == reservation.Id && o.Status != OrderStatus.Cancelled)
                .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
                .Include(o => o.Payments)
                .AsSplitQuery()
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

            var order = await _db.Orders
                 .AsNoTracking()
                 .Include(o => o.Items)
                 .Include(o => o.Payments)
                 .Include(o => o.Restaurant)
                 .Include(o => o.Reservation)
                 .ThenInclude(r => r.Customer)
                 .AsSplitQuery()
                 .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order is null)
                return null;

            var customer = order.Reservation?.Customer;

            if (customer is null)
                throw new DomainException("Customer information is not available for this booking.");

            var normalizedEmail = ApplicationUser.NormalizeEmail(customerEmail);
            var normalizedPhone = ApplicationUser.NormalizePhone(customerPhone);

            if (normalizedEmail != customer.Email || normalizedPhone != customer.Phone)
            {
                throw new DomainException("Customer details do not match this booking.");
            }

            if (order.Status == OrderStatus.Cancelled)
                throw new DomainException("This order was cancelled and cannot be paid.");

            if (order.IsPaid)
                throw new DomainException("This order has already been paid.");

            var now = DateTimeOffset.UtcNow;
            var pending = order.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Pending && now >= p.CreatedDateTime && now - p.CreatedDateTime < SessionLifetime);

            if (pending is not null)
                return ToResult(pending);

            // The bill less the booking advance still unused for this booking
            var bill = order.BillFor(order.Restaurant).Total;
            var credit = await AdvanceCreditAsync(order, ct);
            var due = Money.Create(bill.Amount - credit, bill.Currency);

            string gatewayOrderId;
            string paymentSessionId;
            if (due.Amount > 0m)
            {
                var session = await _gateway.CreateOrderAsync(
                    due.Amount,
                    $"cust_{Guid.NewGuid():N}",
                    customerEmail,
                    customerPhone,
                    returnUrl,
                    ct);

                gatewayOrderId = session.OrderId;
                paymentSessionId = session.PaymentSessionId;
            }
            else
            {
                // The booking advance covers the whole bill, so nothing is sent to the gateway
                gatewayOrderId = $"adv_{Guid.NewGuid():N}";
                paymentSessionId = "booking-advance";
            }

            // Same amount as the stored payment, so what is stored always matches what the guest was charged
            var payment = new Payment(order, due, gatewayOrderId, paymentSessionId, key, customerEmail, credit);
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

            // Nothing to charge: settle the order now. No gateway call and no receipt email for zero rupees.
            if (due.Amount == 0m)
            {
                payment.MarkAsPaid();
                payment.ClearDomainEvents();
                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "Payment {GatewayOrderId} created for order {OrderId} (amount {Amount})",
                payment.GatewayOrderId, orderId, payment.Amount.Amount);

            return ToResult(payment);
        }

        public async Task<Payment> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var payment = await _db.Payments.Matching(new PaymentByGatewayOrderIdSpecification(gatewayOrderId)).FirstOrDefaultAsync(ct);
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

            var payment = await _db.Payments.AsNoTracking().Matching(new PaymentByGatewayOrderIdSpecification(gatewayOrderId)).FirstOrDefaultAsync(ct);
            if (payment is null)
            {
                _logger.LogWarning("Webhook for unknown payment {GatewayOrderId}", gatewayOrderId);
                return true;
            }

            // The signature proves Cashfree sent it, not that the customer paid the right amount
            if (paidAmount != payment.Amount.Amount)
            {
                _logger.LogError(
                    "Amount mismatch for payment {GatewayOrderId}: expected {Expected}, webhook reported {Paid}",
                    gatewayOrderId, payment.Amount.Amount, paidAmount);
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
            var payment = await _db.Payments.Matching(new PaymentByGatewayOrderIdSpecification(gatewayOrderId)).FirstOrDefaultAsync(ct);
            if (payment is null || !payment.MarkAsPaid())
                return false;

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Payment {GatewayOrderId} marked paid", gatewayOrderId);

            // The receipt email is a side effect of the saved change. It runs from the domain event.
            await _events.DispatchAsync(payment.DomainEvents, ct);
            payment.ClearDomainEvents();

            return true;
        }

        public async Task<Payment> RefundAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var payment = await _db.Payments.Matching(new PaymentByGatewayOrderIdSpecification(gatewayOrderId)).FirstOrDefaultAsync(ct);
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
                    payment.GatewayOrderId, refundId, payment.Amount.Amount, "Refund issued by merchant", ct);
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
                        new RefundIssuedEmailModel(payment.CustomerEmail, payment.GatewayOrderId, payment.Amount.Amount), ct);
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
            new PaymentOrderResult(payment.GatewayOrderId, payment.PaymentSessionId, payment.Amount.Amount, payment.Amount.Currency);
    }
}
