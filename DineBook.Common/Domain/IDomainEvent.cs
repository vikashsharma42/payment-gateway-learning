using System;

namespace DineBook.Common.Domain
{
    /// <summary>
    /// Something that happened inside an aggregate. Aggregates collect them; nothing dispatches them yet.
    /// </summary>
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
