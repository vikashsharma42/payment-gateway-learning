using ProductShop.Domain.Entities;
using ProductShop.Payments.Gateway;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Application.Services.Interface
{
    /// <summary>
    /// Payment use cases: create an order, check its status, handle the webhook, and refund.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Creates a gateway order for the product's server-side price and stores a pending Order.
        /// The same idempotency key always returns the same order.
        /// Returns null when the product does not exist.
        /// </summary>
        Task<PaymentOrderResult> CreateOrderAsync(
            int productId,
            string customerEmail,
            string customerPhone,
            string returnUrl,
            string idempotencyKey,
            CancellationToken ct = default);

        /// <summary>
        /// Confirms the status with the gateway and marks the order paid if needed.
        /// Returns null when the order does not exist.
        /// </summary>
        Task<Orders> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default);

        /// <summary>
        /// Verifies the webhook signature and marks the order paid (idempotent).
        /// Returns false when the signature is invalid.
        /// </summary>
        Task<bool> HandleWebhookAsync(string rawBody, string timestamp, string signature, CancellationToken ct = default);

        /// <summary>
        /// Marks a pending order paid and sends the receipt email. Returns true only when the status changed.
        /// </summary>
        Task<bool> MarkPaidAsync(string gatewayOrderId, CancellationToken ct = default);

        /// <summary>
        /// Refunds a paid order in full. Safe to call again: it resumes or re-checks an earlier attempt.
        /// Returns null when the order does not exist.
        /// </summary>
        Task<Orders> RefundAsync(string gatewayOrderId, CancellationToken ct = default);
    }
}