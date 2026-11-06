namespace DineBook.API.DataObjects.Response
{
    public class CreateOrderResponse
    {
        public string OrderId { get; set; }

        public string PaymentSessionId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }
    }
}