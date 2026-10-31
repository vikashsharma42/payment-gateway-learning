using Microsoft.AspNetCore.Mvc;
using ProductShop.API.DataObjects.Request;
using ProductShop.API.DataObjects.Response;
using ProductShop.Application.Services.Interface;
using ProductShop.Domain.Enum;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _payments;

        public PaymentController(IPaymentService payments)
        {
            _payments = payments;
        }

        [HttpPost("create-order")]
        public async Task<ActionResult<CreateOrderResponse>> CreateOrderAssync([FromBody] CreateOrderRequest request, CancellationToken ct)
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/";

            var session = await _payments.CreateOrderAsync(
                request.ProductId, request.CustomerEmail, request.CustomerPhone, returnUrl, ct);

            if (session is null)
                return NotFound(new { error = "Product not found" });

            return Ok(new CreateOrderResponse
            {
                OrderId = session.OrderId,
                PaymentSessionId = session.PaymentSessionId,
                Amount = session.Amount,
                Currency = session.Currency
            });
        }

        [HttpGet("status/{orderId}")]
        public async Task<ActionResult<OrderStatusResponse>> StatusAsync(string orderId, CancellationToken ct)
        {
            var order = await _payments.GetOrderStatusAsync(orderId, ct);
            if (order is null)
                return NotFound();

            return Ok(new OrderStatusResponse
            {
                OrderId = order.GatewayOrderId,
                Status = order.Status.ToString(),
                IsPaid = order.Status == OrderStatus.Paid
            });
        }

        // POST api/payments/webhook (called by Cashfree, signature verified on the raw body)
        [HttpPost("webhook")]
        public async Task<IActionResult> WebhookAsync(CancellationToken ct)
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            var timestamp = Request.Headers["x-webhook-timestamp"].ToString();
            var signature = Request.Headers["x-webhook-signature"].ToString();

            var valid = await _payments.HandleWebhookAsync(body, timestamp, signature, ct);

            return valid ? Ok(new { received = true }) : BadRequest("Invalid signature");
        }
    }
}