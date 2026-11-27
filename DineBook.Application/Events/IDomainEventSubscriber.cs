using DineBook.Domain.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Events
{
    // Reacts to one kind of domain event, such as sending an email or writing a log entry
    public interface IDomainEventSubscriber<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
    }
}
