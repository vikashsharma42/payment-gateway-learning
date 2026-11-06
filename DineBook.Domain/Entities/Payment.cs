
using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;

namespace DineBook.Domain.Entities
{
    // One gateway payment attempt for one food order. The gateway order id ties it to Cashfree.
    public class Payment : Entity, IAuditableEntity
    {
        private Payment()
        {
        }

        public Payment(
            Orders order,
            string gatewayOrderId,
            string paymentSessionId,
            string idempotencyKey,
            string customerEmail)
        {
            DomainValidation<Orders>.Ensure(order).IsNotNull();
            DomainValidation<long>.Ensure(order.Id).IsGreaterThan(0);
            DomainValidation<bool>.Ensure(order.Status != OrderStatus.Cancelled)
                .IsTrue("A cancelled order cannot be paid.");
            DomainValidation<bool>.Ensure(order.IsPaid).IsFalse("This order has already been paid.");

            var amount = order.Total();
            DomainValidation<decimal>.Ensure(amount).IsGreaterThan(0);
            DomainValidation<string>.Ensure(gatewayOrderId).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(paymentSessionId).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(idempotencyKey).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(customerEmail).IsNotNullOrWhiteSpace();

            OrderId = order.Id;
            PublicId = Guid.NewGuid();
            Amount = amount;
            GatewayOrderId = gatewayOrderId.Trim();
            PaymentSessionId = paymentSessionId.Trim();
            IdempotencyKey = idempotencyKey.Trim();
            CustomerEmail = customerEmail.Trim();
            Status = PaymentStatus.Pending;
        }

        public long OrderId { get; private set; }

        public Guid PublicId { get; private set; }

        public decimal Amount { get; private set; }

        public string GatewayOrderId { get; private set; }

        public string PaymentSessionId { get; private set; }

        public string IdempotencyKey { get; private set; }

        public string CustomerEmail { get; private set; }

        // Cashfree refund id. Set when a refund is requested, so a retry reuses it.
        public string RefundId { get; private set; }

        public PaymentStatus Status { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Orders Order { get; private set; }

        // Returns true only when the status actually changed.
        // Duplicate webhooks or repeated status checks return false and change nothing.
        public bool MarkAsPaid()
        {
            if (Status == PaymentStatus.Paid)
                return false;

            DomainValidation<PaymentStatus>.Ensure(Status).IsEqualTo(PaymentStatus.Pending);

            Status = PaymentStatus.Paid;
            return true;
        }

        public void MarkAsFailed()
        {
            DomainValidation<PaymentStatus>.Ensure(Status)
                .IsNotEqualTo(PaymentStatus.Paid);

            Status = PaymentStatus.Failed;
        }

        // Records the refund id before calling the gateway. Only paid payments can be refunded.
        public void RecordRefundRequest(string refundId)
        {
            DomainValidation<string>.Ensure(refundId).IsNotNullOrWhiteSpace();
            DomainValidation<PaymentStatus>.Ensure(Status).IsEqualTo(PaymentStatus.Paid);
            DomainValidation<bool>.Ensure(RefundId is null || RefundId == refundId)
                .IsTrue("This payment already has a different refund in progress.");

            RefundId = refundId;
        }

        // Called only after the gateway confirms the refund succeeded.
        public bool MarkAsRefunded()
        {
            if (Status == PaymentStatus.Refunded)
                return false;

            DomainValidation<PaymentStatus>.Ensure(Status).IsEqualTo(PaymentStatus.Paid);

            Status = PaymentStatus.Refunded;
            return true;
        }

        // Used when Cashfree cancels a refund attempt, so a new request can start with a new id.
        public void ClearRefundRequest()
        {
            if (Status == PaymentStatus.Paid)
                RefundId = null;
        }
    }
}
