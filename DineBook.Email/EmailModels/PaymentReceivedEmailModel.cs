namespace DineBook.Email.EmailModels
{
    public sealed class PaymentReceivedEmailModel : BaseEmailModel
    {
        public PaymentReceivedEmailModel(string toAddress, string orderReference, decimal amount)
        {
            ToAddress = toAddress;
            Subject = $"Payment received for order {orderReference}";
            OrderReference = orderReference;
            Amount = FormatMoney(amount);
        }

        public string OrderReference { get; }

        public string Amount { get; }
    }
}