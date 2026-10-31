namespace ProductShop.API.DataObjects.Response
{
    public class OrderStatusResponse
    {
        public string OrderId { get; set; }

        public string Status { get; set; }

        public bool IsPaid { get; set; }
    }
}