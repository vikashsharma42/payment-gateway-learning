using ProductShop.Domain.Common;

namespace ProductShop.Domain.Orders;

public class Order
{
    public Guid PublicId { get; private set; }

    public string GatewayOrderId { get; private set; }


    public decimal Amount { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }


    public static Order Create(int productId, decimal amount, string gatewayOrderId)
    {
        if (amount <= 0) throw new DomainException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(gatewayOrderId)) throw new DomainException("Gateway order id is required.");

        return new Order
        {
            PublicId = Guid.NewGuid(),
            Amount = amount,
            GatewayOrderId = gatewayOrderId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Returns true only when the status actually changed (idempotent for duplicate webhooks)
    public bool MarkAsPaid()
    {
        if (Status == OrderStatus.Paid) return false;
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