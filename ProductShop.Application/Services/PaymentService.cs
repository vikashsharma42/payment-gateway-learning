using Microsoft.EntityFrameworkCore;
using ProductShop.Application.Services.Interface;
using ProductShop.Domain.Entities;
using ProductShop.Infrastructure.Database;
using ProductShop.Payments.Gateway;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Application.Services
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentGateway _gateway;

        public PaymentService(ApplicationDbContext db, IPaymentGateway gateway)
        {
            _db = db;
            _gateway = gateway;
        }

        public async Task<PaymentOrderResult> CreateOrderAsync(
            int productId, string customerEmail, string customerPhone, string returnUrl, CancellationToken ct = default)
        {
            // Price always comes from the database, never from the client
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
            if (product is null)
                return null;

            var session = await _gateway.CreateOrderAsync(
                product.Price,
                $"cust_{Guid.NewGuid():N}",
                customerEmail,
                customerPhone,
                returnUrl,
                ct);

            _db.Orders.Add(new Orders(product.Id, product.Price, session.OrderId));
            await _db.SaveChangesAsync(ct);

            return session;
        }

        public async Task<Orders> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is null)
                return null;

            var remote = await _gateway.GetOrderStatusAsync(gatewayOrderId, ct);
            if (remote.IsPaid && order.MarkAsPaid())
                await _db.SaveChangesAsync(ct);

            return order;
        }

        public async Task<bool> HandleWebhookAsync(string rawBody, string timestamp, string signature, CancellationToken ct = default)
        {
            if (!_gateway.VerifyWebhookSignature(rawBody, timestamp, signature))
                return false;

            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var type) || type.GetString() != "PAYMENT_SUCCESS_WEBHOOK")
                return true; // valid, but an event we do not act on

            var gatewayOrderId = root.GetProperty("data").GetProperty("order").GetProperty("order_id").GetString();
            if (string.IsNullOrWhiteSpace(gatewayOrderId))
                return true;

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.GatewayOrderId == gatewayOrderId, ct);
            if (order is not null && order.MarkAsPaid())
                await _db.SaveChangesAsync(ct);

            return true;
        }
    }
}