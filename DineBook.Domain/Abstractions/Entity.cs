using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DineBook.Domain.Abstractions
{
    public abstract class Entity
    {
        private readonly List<IDomainEvent> _domainEvents = [];

        public virtual long Id { get; protected set; }

        // Events raised by changes to this entity. Dispatched after the change is saved, then cleared.
        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
