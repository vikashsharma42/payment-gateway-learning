namespace ProductShop.Payments.Abstractions
{

    // The app depends only on this interface, not on Cashfree.
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


    public record PaymentOrderResult(string OrderId, string PaymentSessionId, decimal Amount, string Currency);

    public record PaymentStatusResult(string OrderId, string Status)
    {
        public bool IsPaid => Status == "PAID";
    }
}