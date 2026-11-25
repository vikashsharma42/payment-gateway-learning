using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace DineBook.Domain.Entities
{
    public class Location : Entity, IAuditableEntity
    {
        public Location(string name, Address address, string phone)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace("Location name is required.").HasMaxLength(100);
            DomainValidation<Address>.Ensure(address).IsNotNull();
            DomainValidation<string>.Ensure(phone ?? string.Empty, nameof(phone)).HasMaxLength(15);

            Name = name.Trim();
            Address = address;
            Phone = (phone ?? string.Empty).Trim();
            IsActive = true;
        }

        public long RestaurantId { get; internal set; }

        public string Name { get; private set; }

        public Address Address { get; private set; }

        public string Phone { get; private set; }

        // Inactive locations are hidden from staff switching. Their tables stay for past bookings.
        public bool IsActive { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Restaurant Restaurant { get; set; }

        private readonly List<Table> _tables = [];
        public virtual IReadOnlyCollection<Table> Tables => _tables.AsReadOnly();
    }
}