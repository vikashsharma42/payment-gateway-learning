using System;

namespace DineBook.Domain.Abstractions
{
    // A fact that happened in the domain. Handlers in the Application layer react to it after the change is saved.
    public interface IDomainEvent
    {
        DateTimeOffset OccurredAt { get; }
    }
}
