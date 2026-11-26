using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace DineBook.Domain.Entities
{
    public class Location : Entity, IAuditableEntity
    {
        public const int MaxPhoneLength = 15;

        // Used by EF Core when loading. Validation runs only in the public constructor.
        private Location()
        {
        }

        public Location(string name, Address address, string phone)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace("Location name is required.").HasMaxLength(100);
            DomainValidation<Address>.Ensure(address).IsNotNull();
            DomainValidation<string>.Ensure(phone ?? string.Empty, nameof(phone)).HasMaxLength(MaxPhoneLength);

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
        // Changed through Restaurant.SetLocationActive, which keeps at least one location open.
        public bool IsActive { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Restaurant Restaurant { get; set; }

        private readonly List<Table> _tables = [];
        public virtual IReadOnlyCollection<Table> Tables => _tables.AsReadOnly();

        internal void Activate() => IsActive = true;

        internal void Deactivate() => IsActive = false;

        // Called by Restaurant.AddTable, which has already checked the table's rules
        internal void AddTable(Table table)
        {
            DomainValidation<Table>.Ensure(table).IsNotNull();
            _tables.Add(table);
        }
    }
}
