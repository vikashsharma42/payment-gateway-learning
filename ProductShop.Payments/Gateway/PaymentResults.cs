namespace ProductShop.Payments.Gateway
{
    public record PaymentOrderResult(string OrderId, string PaymentSessionId, decimal Amount, string Currency);

    public record PaymentStatusResult(string OrderId, string Status)
    {
        public bool IsPaid => Status == "PAID";
    }
}