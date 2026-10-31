using ProductShop.Domain.Abstractions;
using ProductShop.Domain.Enum;
using ProductShop.Domain.Exceptions;
using System;

namespace ProductShop.Domain.Entities
{
    public class Orders : BaseEntity
    {
        public Orders(
            long productId,
            decimal amount,
            string gatewayOrderId,
            string paymentSessionId,
            string idempotencyKey,
            string customerEmail)
        {
            if (productId <= 0)
                throw new DomainException("Product id is required.");

            if (amount <= 0)
                throw new DomainException("Amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(gatewayOrderId))
                throw new DomainException("Gateway order id is required.");

            if (string.IsNullOrWhiteSpace(paymentSessionId))
                throw new DomainException("Payment session id is required.");

            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new DomainException("Idempotency key is required.");

            if (string.IsNullOrWhiteSpace(customerEmail))
                throw new DomainException("Customer email is required.");

            ProductId = productId;
            PublicId = Guid.NewGuid();
            Amount = amount;
            GatewayOrderId = gatewayOrderId.Trim();
            PaymentSessionId = paymentSessionId.Trim();
            IdempotencyKey = idempotencyKey.Trim();
            CustomerEmail = customerEmail.Trim();
            Status = OrderStatus.Pending;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public long ProductId { get; private set; }

        public Guid PublicId { get; private set; }

        public decimal Amount { get; private set; }

        public string GatewayOrderId { get; private set; }

        public string PaymentSessionId { get; private set; }

        public string IdempotencyKey { get; private set; }

        public string CustomerEmail { get; private set; }

        // Cashfree refund id. Set when a refund is requested, so a retry reuses it.
        public string RefundId { get; private set; }

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

        // Records the refund id before calling the gateway. Only paid orders can be refunded.
        public void RecordRefundRequest(string refundId)
        {
            if (string.IsNullOrWhiteSpace(refundId))
                throw new DomainException("Refund id is required.");

            if (Status != OrderStatus.Paid)
                throw new DomainException("Only paid orders can be refunded.");

            if (RefundId is not null && RefundId != refundId)
                throw new DomainException("This order already has a different refund in progress.");

            RefundId = refundId;
        }

        // Called only after the gateway confirms the refund succeeded.
        public bool MarkAsRefunded()
        {
            if (Status == OrderStatus.Refunded)
                return false;

            if (Status != OrderStatus.Paid)
                throw new DomainException("Only paid orders can be marked as refunded.");

            Status = OrderStatus.Refunded;
            return true;
        }

        // Used when Cashfree cancels a refund attempt, so a new request can start with a new id.
        public void ClearRefundRequest()
        {
            if (Status == OrderStatus.Paid)
                RefundId = null;
        }
    }
}