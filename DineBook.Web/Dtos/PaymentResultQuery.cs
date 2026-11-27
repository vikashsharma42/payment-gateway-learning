using Microsoft.AspNetCore.Mvc;

namespace DineBook.Web.Dtos
{
    // Query string sent back by Cashfree after a payment. The gateway names the field order_id.
    public class PaymentResultQuery
    {
        [FromQuery(Name = "order_id")]
        public string OrderId { get; set; }
    }
}
