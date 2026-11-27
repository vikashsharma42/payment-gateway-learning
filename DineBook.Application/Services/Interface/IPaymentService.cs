using DineBook.Domain.Entities;
using System.Collections.Generic;
using DineBook.Payments.Gateway;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services.Interface
{
    /// <summary>
    /// Payment use cases: bill a booking, create a payment order, check its status, handle the webhook, and refund.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Returns the food orders of a booking that can be paid (not cancelled), with their items and payments.
        /// The caller must give the booking's phone number. Throws when the booking or phone does not match.
        /// </summary>
        Task<IReadOnlyList<Orders>> GetPayableOrdersAsync(string reservationCode, string phone, CancellationToken ct = default);

        /// <summary>
        /// Creates a gateway order for one food order's server-side total and stores a pending Payment.
        /// The same idempotency key always returns the same order. A pending payment that is still
        /// fresh is reused, so two checkouts for one order cannot both be charged.
        /// Returns null when the food order does not exist.
        /// </summary>
        Task<PaymentOrderResult> CreateOrderAsync(
            long orderId,
            string customerEmail,
            string customerPhone,
            string returnUrl,
            string idempotencyKey,
            CancellationToken ct = default);

        /// <summary>
        /// Confirms the status with the gateway and marks the order paid if needed.
        /// Returns null when the order does not exist.
        /// </summary>
        Task<Payment> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default);

        /// <summary>
        /// Verifies the webhook signature and marks the order paid (idempotent).
        /// Returns false when the signature is invalid.
        /// </summary>
        Task<bool> HandleWebhookAsync(string rawBody, string timestamp, string signature, CancellationToken ct = default);

        /// <summary>
        /// Marks a pending payment paid and sends the receipt email. Returns true only when the status changed.
        /// </summary>
        Task<bool> MarkPaidAsync(string gatewayOrderId, CancellationToken ct = default);

        /// <summary>
        /// Refunds a paid order in full. Safe to call again: it resumes or re-checks an earlier attempt.
        /// Returns null when the order does not exist.
        /// </summary>
        Task<Payment> RefundAsync(string gatewayOrderId, CancellationToken ct = default);

        /// <summary>
        /// Returns one food order for the signed-in customer, with its items, payments, restaurant and booking.
        /// Returns null when the order is not at this restaurant or does not belong to this customer,
        /// so other people's bills are never revealed.
        /// </summary>
        Task<Orders> GetOrderForCustomerAsync(long orderId, string restaurantSlug, long customerId, CancellationToken ct = default);

        /// <summary>
        /// The booking code and restaurant address of a food order, for the "back to your booking" links.
        /// Returns null when the order does not exist.
        /// </summary>
        Task<PaymentBookingLinks> GetBookingLinksAsync(long orderId, CancellationToken ct = default);
    }

    public sealed record PaymentBookingLinks(string ReservationCode, string RestaurantSlug);
}
