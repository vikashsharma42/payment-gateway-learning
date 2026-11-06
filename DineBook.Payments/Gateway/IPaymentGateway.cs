using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Payments.Gateway
{
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

        // Refund ids must be unique per order. Reusing one returns an error, which makes retries safe.
        Task<RefundResult> CreateRefundAsync(
            string orderId,
            string refundId,
            decimal amount,
            string note,
            CancellationToken ct = default);

        Task<RefundResult> GetRefundStatusAsync(string orderId, string refundId, CancellationToken ct = default);

        bool VerifyWebhookSignature(string rawBody, string timestamp, string signature);
    }
}
