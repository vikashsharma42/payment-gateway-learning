using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductShop.Domain.Common;
using ProductShop.Domain.Orders;
using ProductShop.Infrastructure.Persistence;
using ProductShop.Payments.Abstractions;

namespace ProductShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(
    AppDbContext db,
    IPaymentGateway gateway,
    ILogger<PaymentsController> logger) : ControllerBase
{
    public record CreateOrderRequest(int ProductId, string CustomerEmail, string CustomerPhone);

    // 1. Create order. Price comes from the database, never from the client.
    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var product = await db.Products.FindAsync(new object[] { request.ProductId }, ct);
        if (product is null)
            return NotFound(new { error = "Product not found" });

        try
        {
            var session = await gateway.CreateOrderAsync(
                product.Price,
                $"cust_{Guid.NewGuid():N}",
                request.CustomerEmail,
                request.CustomerPhone,
                $"{Request.Scheme}://{Request.Host}/",
                ct);

            var order = Order.Create(product.Id, product.Price, session.OrderId);
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);

            return Ok(new
            {
                orderId = session.OrderId,
                paymentSessionId = session.PaymentSessionId,
                amount = session.Amount,
                currency = session.Currency
            });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create payment order");
            return StatusCode(500, new { error = "Failed to create payment order" });
        }
    }

    // 2. Status: confirm with Cashfree, then update the database (idempotent)
    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> Status(string orderId, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.GatewayOrderId == orderId, ct);
        if (order is null) return NotFound();

        var remote = await gateway.GetOrderStatusAsync(orderId, ct);
        if (remote.IsPaid && order.MarkAsPaid())
            await db.SaveChangesAsync(ct);

        return Ok(new
        {
            orderId,
            status = order.Status.ToString(),
            isPaid = order.Status == OrderStatus.Paid
        });
    }

    // 3. Webhook: verify signature on the raw body, then update the database (idempotent)
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["x-webhook-signature"].ToString();
        var timestamp = Request.Headers["x-webhook-timestamp"].ToString();

        if (!gateway.VerifyWebhookSignature(body, timestamp, signature))
            return BadRequest("Invalid signature");

        using var doc = JsonDocument.Parse(body);
        var type = doc.RootElement.GetProperty("type").GetString();

        if (type == "PAYMENT_SUCCESS_WEBHOOK")
        {
            var gatewayOrderId = doc.RootElement
                .GetProperty("data").GetProperty("order")
                .GetProperty("order_id").GetString();

            var order = await db.Orders.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is not null && order.MarkAsPaid())
                await db.SaveChangesAsync(ct);
        }

        return Ok(new { received = true });
    }
}
