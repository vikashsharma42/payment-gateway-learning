namespace DineBook.Payments.Gateway
{
    public record PaymentOrderResult(string OrderId, string PaymentSessionId, decimal Amount, string Currency);

    public record PaymentStatusResult(string OrderId, string Status)
    {
        public bool IsPaid => Status == "PAID";
    }

    public record RefundResult(string RefundId, string Status)
    {
        public bool IsSuccess => Status == "SUCCESS";
    }
}
