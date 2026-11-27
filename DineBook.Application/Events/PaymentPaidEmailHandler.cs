using DineBook.Domain.Events;
using DineBook.Email;
using DineBook.Email.EmailModels;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Events
{
    // Sends the payment receipt once the gateway confirms the payment
    public sealed class PaymentPaidEmailHandler(IEmailService email, ILogger<PaymentPaidEmailHandler> logger) : IDomainEventSubscriber<PaymentPaidEvent>
    {
        public async Task HandleAsync(PaymentPaidEvent domainEvent, CancellationToken ct = default)
        {
            try
            {
                await email.SendAsync(
                    new PaymentReceivedEmailModel(domainEvent.CustomerEmail, domainEvent.GatewayOrderId, domainEvent.Amount), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed email must never undo a payment that already succeeded
                logger.LogWarning(ex, "Receipt for payment {GatewayOrderId} could not be sent", domainEvent.GatewayOrderId);
            }
        }
    }
}