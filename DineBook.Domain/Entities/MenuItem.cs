using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using System;

namespace DineBook.Domain.Entities
{
    // A dish on one restaurant's menu. Orders copy the name, price and image, so later edits do not change past orders.
    // Added through Restaurant.AddMenuItem.
    public class MenuItem : Entity, IAuditableEntity
    {
        public const int MaxImageUrlLength = 500;

        public MenuItem(string name, string description, decimal price, string imageUrl = null)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(100);
            DomainValidation<string>.Ensure(description ?? string.Empty, nameof(description)).HasMaxLength(300);
            EnsurePrice(price);

            Name = name.Trim();
            Description = (description ?? string.Empty).Trim();
            Price = price;
            ImageUrl = NormalizeImage(imageUrl);
            Category = string.Empty;
            IsAvailable = true;
        }

        public long RestaurantId { get; private set; }

        public long MenuCategoryId { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        // Name of the category, copied so listings and past orders read it without a join
        public string Category { get; private set; }

        public decimal Price { get; private set; }

        // Photo shown in the menu and order history. Null means the page shows a placeholder.
        public string ImageUrl { get; private set; }

        // Unavailable items cannot be ordered, but they stay on record
        public bool IsAvailable { get; private set; }

        public bool IsDeleted { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Restaurant Restaurant { get; private set; }

        public virtual MenuCategory MenuCategory { get; private set; }

        public void ChangePrice(decimal price)
        {
            EnsurePrice(price);

            Price = price;
        }

        public void ChangeImage(string imageUrl) => ImageUrl = NormalizeImage(imageUrl);

        public void SetAvailability(bool available) => IsAvailable = available;

        // Soft delete: the item leaves the menu, but past orders still point at it
        public void Delete()
        {
            if (IsDeleted)
                return;

            IsDeleted = true;
            IsAvailable = false;
            DeletedAt = DateTimeOffset.UtcNow;
        }

        // Called by MenuCategory when the item is added to it
        internal void SetCategoryName(string categoryName) => Category = categoryName;

        private static string NormalizeImage(string imageUrl)
        {
            var value = (imageUrl ?? string.Empty).Trim();
            DomainValidation<string>.Ensure(value, nameof(imageUrl)).HasMaxLength(MaxImageUrlLength);

            return value.Length == 0 ? null : value;
        }

        private static void EnsurePrice(decimal price) =>
            DomainValidation<decimal>.Ensure(price)
                .IsGreaterThan(0m, "Price must be greater than zero and at most 100000.")
                .IsLessThanOrEqualTo(100000m, "Price must be greater than zero and at most 100000.");
    }
}
