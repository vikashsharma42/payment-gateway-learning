using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace DineBook.Domain.Entities
{
    public class Location : Entity, IAuditableEntity
    {
        private Location()
        {
            // EF Core constructor
        }

        public Location(string name, Address address, string phone)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace("Location name is required.").HasMaxLength(100);
            DomainValidation<Address>.Ensure(address).IsNotNull();
            DomainValidation<string>.Ensure(phone ?? string.Empty, nameof(phone)).HasMaxLength(15);

            Name = name.Trim();
            Slug = RestaurantSlug.FromName(Name);
            Address = address;
            Phone = (phone ?? string.Empty).Trim();
            IsActive = true;
        }

        public long RestaurantId { get; internal set; }

        public string Name { get; private set; }

        public string Slug { get; private set; }

        public bool IsDefault { get; private set; }

        public Address Address { get; private set; }

        public string Phone { get; private set; }

        public bool IsActive { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Restaurant Restaurant { get; set; }

        private readonly List<Table> _tables = [];
        public virtual IReadOnlyCollection<Table> Tables => _tables.AsReadOnly();

        public void Rename(string name)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace("Location name is required.").HasMaxLength(100);
            Name = name.Trim();

            // Tables copy the location name for display, so keep their copy in step
            foreach (var table in _tables)
                table.SetLocationName(Name);
        }

        public void UpdateContact(Address address, string phone)
        {
            DomainValidation<Address>.Ensure(address, nameof(address)).IsNotNull();
            var trimmedPhone = (phone ?? string.Empty).Trim();
            DomainValidation<string>.Ensure(trimmedPhone, nameof(phone)).HasMaxLength(15);

            Address = address;
            Phone = trimmedPhone;
        }

        internal void SetSlug(string slug)
        {
            DomainValidation<bool>.Ensure(RestaurantSlug.IsValid(slug)).IsTrue("The location URL is not valid.");
            Slug = slug;
        }

        internal void SetDefault(bool value) => IsDefault = value;

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
