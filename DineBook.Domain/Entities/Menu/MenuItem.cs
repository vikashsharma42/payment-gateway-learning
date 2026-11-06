using System;
using System.ComponentModel.DataAnnotations.Schema;
using DineBook.Domain.Abstractions;
using DineBook.Domain.Entities.Organization;
using DineBook.Domain.Exceptions;

namespace DineBook.Domain.Entities.Menu
{
    /// <summary>
    /// A dish on one restaurant's menu. Orders copy the name and price, so later edits do not change past orders.
    /// Added through <see cref="Restaurant.AddMenuItem"/>.
    /// </summary>
    public class MenuItem : AggregateRoot, IAuditableEntity
    {
        public const int MaxNameLength = 100;
        public const int MaxDescriptionLength = 300;
        public const int MaxCategoryLength = MenuCategory.MaxNameLength;
        public const decimal MaxPrice = 100000m;

        public MenuItem(string name, string description, decimal price)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<string>.Ensure(description ?? string.Empty, nameof(description)).HasMaxLength(MaxDescriptionLength);
            EnsurePrice(price);

            Name = name.Trim();
            Description = (description ?? string.Empty).Trim();
            Price = price;
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

        // Unavailable items cannot be ordered, but they stay on record
        public bool IsAvailable { get; private set; }

        public bool IsDeleted { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public virtual Restaurant Restaurant { get; private set; }

        [ForeignKey(nameof(MenuCategoryId))]
        public virtual MenuCategory MenuCategory { get; private set; }

        public void ChangePrice(decimal price)
        {
            EnsurePrice(price);

            Price = price;
        }

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

        private static void EnsurePrice(decimal price) =>
            DomainValidation<decimal>.Ensure(price)
                .IsGreaterThan(0m, "Price must be greater than zero and at most 100000.")
                .IsLessThanOrEqualTo(MaxPrice, "Price must be greater than zero and at most 100000.");
    }
}
