using DineBook.Domain.Abstractions;
using DineBook.Domain.Events;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;

namespace DineBook.Domain.Entities
{
    // One gateway payment attempt for one food order. The gateway order id ties it to Cashfree.
    public class Payment : Entity, IAuditableEntity
    {
        // Used by EF Core when loading. Validation runs only in the public constructor.
        private Payment()
        {
        }

        public Payment(
            Orders order,
            Money amount,
            string gatewayOrderId,
            string paymentSessionId,
            string idempotencyKey,
            string customerEmail,
            decimal advanceApplied = 0m)
        {
            DomainValidation<Orders>.Ensure(order).IsNotNull();
            DomainValidation<long>.Ensure(order.Id).IsGreaterThan(0);
            DomainValidation<bool>.Ensure(order.Status != OrderStatus.Cancelled).IsTrue("A cancelled order cannot be paid.");
            DomainValidation<bool>.Ensure(order.IsPaid).IsFalse("This order has already been paid.");

            // The amount is what is still due after the booking advance, see PaymentService.AdvanceCreditAsync.
            // It is zero when the advance covers the whole bill.
            DomainValidation<decimal>.Ensure(amount.Amount).IsGreaterThanOrEqualTo(0m, "Amount cannot be negative.");
            DomainValidation<decimal>.Ensure(advanceApplied).IsGreaterThanOrEqualTo(0m, "Advance cannot be negative.");
            DomainValidation<string>.Ensure(gatewayOrderId).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(paymentSessionId).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(idempotencyKey).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(customerEmail).IsNotNullOrWhiteSpace();

            OrderId = order.Id;
            ReservationId = order.ReservationId;
            PublicId = Guid.NewGuid();
            Amount = amount;
            AdvanceApplied = advanceApplied;
            GatewayOrderId = gatewayOrderId.Trim();
            PaymentSessionId = paymentSessionId.Trim();
            IdempotencyKey = idempotencyKey.Trim();
            CustomerEmail = customerEmail.Trim();
            Status = PaymentStatus.Pending;
        }

        // Null for a booking deposit, which is not tied to a food order
        public long? OrderId { get; internal set; }

        // The booking this payment belongs to. Set for food payments and for deposits.
        public long? ReservationId { get; internal set; }

        // Pays one booking's deposit, made when the booking is created. Not tied to a food order.
        public Payment(
            long reservationId,
            Money amount,
            string gatewayOrderId,
            string paymentSessionId,
            string idempotencyKey,
            string customerEmail)
        {
            DomainValidation<long>.Ensure(reservationId).IsGreaterThan(0);
            DomainValidation<decimal>.Ensure(amount.Amount).IsGreaterThan(0);
            DomainValidation<string>.Ensure(gatewayOrderId).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(paymentSessionId).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(idempotencyKey).IsNotNullOrWhiteSpace();
            DomainValidation<string>.Ensure(customerEmail).IsNotNullOrWhiteSpace();

            ReservationId = reservationId;
            PublicId = Guid.NewGuid();
            Amount = amount;
            GatewayOrderId = gatewayOrderId.Trim();
            PaymentSessionId = paymentSessionId.Trim();
            IdempotencyKey = idempotencyKey.Trim();
            CustomerEmail = customerEmail.Trim();
            Status = PaymentStatus.Pending;
        }

        public Guid PublicId { get; private set; }

        public Money Amount { get; private set; }

        // Part of the booking advance this payment used. Zero when none.
        public decimal AdvanceApplied { get; private set; }

        public string GatewayOrderId { get; private set; }

        public string PaymentSessionId { get; private set; }

        public string IdempotencyKey { get; private set; }

        public string CustomerEmail { get; private set; }

        // Cashfree refund id. Set when a refund is requested, so a retry reuses it.
        public string RefundId { get; private set; }

        public PaymentStatus Status { get; private set; }

        // Money already sent back to the customer (partial refunds, such as a deposit minus the cancellation fee)
        public decimal RefundedAmount { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Orders Order { get; private set; }

        // Returns false when already paid. A failed attempt can still be confirmed paid, because the gateway is the source of truth.
        public bool MarkAsPaid()
        {
            if (Status == PaymentStatus.Paid)
                return false;

            DomainValidation<PaymentStatus>.Ensure(Status).IsNotEqualTo(PaymentStatus.Refunded);
            Status = PaymentStatus.Paid;
            AddDomainEvent(new PaymentPaidEvent(Id, GatewayOrderId, CustomerEmail, Amount.Amount, DateTimeOffset.UtcNow));
            return true;
        }

        public void MarkAsFailed()
        {
            DomainValidation<PaymentStatus>.Ensure(Status).IsNotEqualTo(PaymentStatus.Paid);

            Status = PaymentStatus.Failed;
        }

        // Records the refund id before calling the gateway. Only paid payments can be refunded.
        public void RecordRefundRequest(string refundId)
        {
            DomainValidation<string>.Ensure(refundId).IsNotNullOrWhiteSpace();
            DomainValidation<PaymentStatus>.Ensure(Status).IsEqualTo(PaymentStatus.Paid);
            DomainValidation<bool>.Ensure(RefundId is null || RefundId == refundId).IsTrue("This payment already has a different refund in progress.");

            RefundId = refundId;
        }

        // Called only after the gateway confirms the refund succeeded.
        public bool MarkAsRefunded()
        {
            if (Status == PaymentStatus.Refunded)
                return false;

            DomainValidation<PaymentStatus>.Ensure(Status).IsEqualTo(PaymentStatus.Paid);

            Status = PaymentStatus.Refunded;
            RefundedAmount = Amount.Amount;
            return true;
        }

        // Records money sent back without refunding the whole payment (the payment stays Paid)
        public void RecordPartialRefund(decimal amount)
        {
            DomainValidation<PaymentStatus>.Ensure(Status).IsEqualTo(PaymentStatus.Paid, "Only paid payments can be refunded.");
            DomainValidation<bool>.Ensure(amount > 0m).IsTrue("The refund must be more than zero.");
            DomainValidation<bool>.Ensure(RefundedAmount + amount <= Amount.Amount).IsTrue("The refund is more than the amount paid.");

            RefundedAmount += amount;
        }

        // Used when Cashfree cancels a refund attempt, so a new request can start with a new id.
        public void ClearRefundRequest()
        {
            if (Status == PaymentStatus.Paid)
                RefundId = null;
        }
    }
}
