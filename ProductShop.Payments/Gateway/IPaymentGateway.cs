using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Payments.Gateway
{
    /// <summary>
    /// Gateway-agnostic contract. Application code depends on this interface only,
    /// so another provider (Razorpay, Stripe, ...) can be added as a new class.
    /// </summary>
    public interface IPaymentGateway
    {
        Task<PaymentOrderResult> CreateOrderAsync(
            decimal amount,
            string customerId,
            string customerEmail,
            string customerPhone,
            string returnUrl,
            CancellationToken ct = default);

        Task<PaymentStatusResult> GetOrderStatusAsync(string orderId, CancellationToken ct = default);

        bool VerifyWebhookSignature(string rawBody, string timestamp, string signature);
    }
}