using ProductShop.Domain.Entities;
using ProductShop.Payments.Gateway;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Application.Services.Interface
{
    /// <summary>
    /// Payment use cases: create an order, check its status, and handle the gateway webhook.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Creates a gateway order for the product's server-side price and stores a pending Order.
        /// Returns null when the product does not exist.
        /// </summary>
        Task<PaymentOrderResult> CreateOrderAsync(
            int productId,
            string customerEmail,
            string customerPhone,
            string returnUrl,
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
    }
}