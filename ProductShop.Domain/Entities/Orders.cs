using ProductShop.Domain.Abstractions;
using ProductShop.Domain.Enum;
using ProductShop.Domain.Exceptions;
using System;

namespace ProductShop.Domain.Entities
{
    public class Orders : BaseEntity
    {
        public Orders(long productId, decimal amount, string gatewayOrderId)
        {
            if (productId <= 0)
                throw new DomainException("Product id is required.");

            if (amount <= 0)
                throw new DomainException("Amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(gatewayOrderId))
                throw new DomainException("Gateway order id is required.");

            ProductId = productId;
            PublicId = Guid.NewGuid();
            Amount = amount;
            GatewayOrderId = gatewayOrderId.Trim();
            Status = OrderStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public long ProductId { get; private set; }

        public Guid PublicId { get; private set; }

        public decimal Amount { get; private set; }

        public string GatewayOrderId { get; private set; }

        public OrderStatus Status { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        // Returns true only when the status actually changed.
        // Duplicate webhooks or repeated status checks return false and change nothing.
        public bool MarkAsPaid()
        {
            if (Status == OrderStatus.Paid)
                return false;

            if (Status != OrderStatus.Pending)
                throw new DomainException("Only pending orders can be marked as paid.");

            Status = OrderStatus.Paid;
            return true;
        }

        public void MarkAsFailed()
        {
            if (Status == OrderStatus.Paid)
                throw new DomainException("A paid order cannot fail.");

            Status = OrderStatus.Failed;
        }
    }
}