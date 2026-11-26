using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;

namespace DineBook.Domain.Entities
{
    // A dish on one restaurant's menu. Orders copy the name and price, so later edits do not change past orders.
    // Added through Restaurant.AddMenuItem.
    public class MenuItem : Entity, IAuditableEntity
    {
        // Used by EF Core when loading. Validation runs only in the public constructor.
        private MenuItem()
        {
        }

        public MenuItem(string name, string description, decimal price, string imageUrl = null)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(100);
            DomainValidation<string>.Ensure(description ?? string.Empty, nameof(description)).HasMaxLength(300);
            EnsurePrice(price);

            Name = name.Trim();
            Description = (description ?? string.Empty).Trim();
            Price = Money.Create(price);
            Category = string.Empty;
            ImageUrl = NormalizeImage(imageUrl);
            IsAvailable = true;
        }

        public long RestaurantId { get; internal set; }

        public long MenuCategoryId { get; internal set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        // Filled in from the menu category when the item is added
        public string Category { get; private set; }

        public Money Price { get; private set; }

        public string ImageUrl { get; private set; }

        public bool IsAvailable { get; private set; }

        // Set by Remove. Removed items stay in past orders, but are not offered on the menu.
        public DateTimeOffset? DeletedAt { get; private set; }

        public bool IsDeleted => DeletedAt is not null;

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Restaurant Restaurant { get; private set; }

        public virtual MenuCategory MenuCategory { get; private set; }

        public void ChangePrice(decimal price)
        {
            EnsurePrice(price);
            Price = Money.Create(price);
        }

        public void ChangeImage(string imageUrl) => ImageUrl = NormalizeImage(imageUrl);

        public void SetAvailability(bool available)
        {
            DomainValidation<bool>.Ensure(IsDeleted).IsFalse("A removed menu item cannot be changed.");

            IsAvailable = available;
        }

        // Soft delete: the item is hidden from the menu and can no longer be ordered
        public void Remove()
        {
            if (IsDeleted)
                return;

            DeletedAt = DateTimeOffset.UtcNow;
            IsAvailable = false;
        }

        internal void SetCategoryName(string categoryName) => Category = categoryName;

        private static string NormalizeImage(string imageUrl)
        {
            var value = (imageUrl ?? string.Empty).Trim();
            DomainValidation<string>.Ensure(value, nameof(imageUrl)).HasMaxLength(50);

            return value.Length == 0 ? null : value;
        }

        private static void EnsurePrice(decimal price) =>
            DomainValidation<decimal>.Ensure(price)
                .IsGreaterThan(0m, "Price must be greater than zero and at most 100000.")
                .IsLessThanOrEqualTo(100000m, "Price must be greater than zero and at most 100000.");
    }
}
