using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;

namespace DineBook.Domain.Entities
{
    // A table inside one restaurant. Added through Restaurant.AddTable, which keeps labels unique.
    // Removal is a soft delete, so past bookings keep their table.
    public class RestaurantTable : Entity, IAuditableEntity
    {
        public const int MaxLabelLength = 20;
        public const int MaxLocationLength = 50;
        public const int MinCapacity = 1;
        public const int MaxCapacity = 20;

        public RestaurantTable(string label, int capacity, string location)
        {
            DomainValidation<string>.Ensure(label).IsNotNullOrWhiteSpace().HasMaxLength(MaxLabelLength);
            DomainValidation<int>.Ensure(capacity)
                .IsBetween(MinCapacity, MaxCapacity, "Table capacity must be between 1 and 20 guests.");
            DomainValidation<string>.Ensure(location ?? string.Empty, nameof(location)).HasMaxLength(MaxLocationLength);

            Label = label.Trim();
            Capacity = capacity;
            Location = (location ?? string.Empty).Trim();
            Status = TableStatus.Available;
        }

        public long RestaurantId { get; private set; }

        public string Label { get; private set; }

        public int Capacity { get; private set; }

        // Free text, for example "window" or "patio"
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
            DomainValidation<string>.Ensure(label).IsNotNullOrWhiteSpace().HasMaxLength(MaxLabelLength);

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
