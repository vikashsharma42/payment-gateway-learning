using DineBook.Domain.Abstractions;
using System;

namespace DineBook.Domain.Events
{
    // The gateway confirmed a payment. The receipt email is sent from this event.
    public sealed record PaymentPaidEvent(
        long PaymentId,
        string GatewayOrderId,
        string CustomerEmail,
        decimal Amount,
        DateTimeOffset OccurredAt) : IDomainEvent;
}
