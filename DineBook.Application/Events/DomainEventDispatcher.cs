using DineBook.Domain.Abstractions;
using DineBook.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Events
{
    public sealed class DomainEventDispatcher(IServiceProvider services, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
    {
        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                // Each event type is listed here explicitly, so a new event needs a new case
                switch (domainEvent)
                {
                    case PaymentPaidEvent paid:
                        await DispatchToHandlersAsync(paid, ct);
                        break;
                    default:
                        logger.LogWarning("No handler is registered for domain event {EventType}", domainEvent.GetType().Name);
                        break;
                }
            }
        }

        // A failing handler is logged and skipped. The change is already saved, so it must not be undone.
        private async Task DispatchToHandlersAsync<TEvent>(TEvent domainEvent, CancellationToken ct) where TEvent : IDomainEvent
        {
            foreach (var handler in services.GetServices<IDomainEventSubscriber<TEvent>>())
            {
                try
                {
                    await handler.HandleAsync(domainEvent, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Handler {Handler} failed for {EventType}", handler.GetType().Name, typeof(TEvent).Name);
                }
            }
        }
    }
}