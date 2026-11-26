using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;

namespace DineBook.Domain.Entities
{
    // A table inside one location of a restaurant. Added through Restaurant.AddTable, which keeps labels unique per location.
    // Removal is a soft delete, so past bookings keep their table.
    public class Table : Entity, IAuditableEntity
    {
        public Table(string label, int capacity)
        {
            DomainValidation<string>.Ensure(label).IsNotNullOrWhiteSpace().HasMaxLength(20);
            DomainValidation<int>.Ensure(capacity).IsBetween(1, 20, "Table capacity must be between 1 and 20 guests.");

            Label = label.Trim();
            Capacity = capacity;
            Location = string.Empty;
            Status = TableStatus.Available;
        }

        public long RestaurantId { get; internal set; }

        // The location this table is at. Set by Restaurant.AddTable.
        public long LocationId { get; internal set; }

        public string Label { get; private set; }

        public int Capacity { get; private set; }

        // Name of the location this table is at. Set by Restaurant.AddTable.
        public string Location { get; private set; }

        public TableStatus Status { get; private set; }

        public bool IsDeleted { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        // Tables that are out of service are not offered for new bookings
        public bool IsActive => Status != TableStatus.OutOfService;

        public virtual Restaurant Restaurant { get; private set; }

        // Called by Restaurant.AddTable only
        internal void AssignLocation(Location location)
        {
            LocationId = location.Id;
            Location = location.Name;
        }

        public void Activate()
        {
            DomainValidation<bool>.Ensure(IsDeleted).IsFalse("A removed table cannot be activated.");

            Status = TableStatus.Available;
        }

        public void Deactivate() => Status = TableStatus.OutOfService;

        public void Occupy()
        {
            DomainValidation<bool>.Ensure(IsActive).IsTrue("A table that is out of service cannot be occupied.");

            Status = TableStatus.Occupied;
        }

        public void Release()
        {
            if (Status == TableStatus.Occupied)
                Status = TableStatus.Available;
        }

        public void Rename(string label)
        {
            DomainValidation<string>.Ensure(label).IsNotNullOrWhiteSpace().HasMaxLength(20);

            Label = label.Trim();
        }

        public void Delete()
        {
            if (IsDeleted)
                return;

            IsDeleted = true;
            Status = TableStatus.OutOfService;
            DeletedAt = DateTimeOffset.UtcNow;
        }
    }
}
