namespace ProductShop.Email.EmailModels
{
    public sealed class RefundIssuedEmailModel : BaseEmailModel
    {
        public RefundIssuedEmailModel(string toAddress, string orderReference, decimal amount)
        {
            ToAddress = toAddress;
            Subject = $"Refund issued for order {orderReference}";
            OrderReference = orderReference;
            Amount = FormatMoney(amount);
        }

        public string OrderReference { get; }

        public string Amount { get; }
    }
}