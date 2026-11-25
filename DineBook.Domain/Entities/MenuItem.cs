using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using System;

namespace DineBook.Domain.Entities
{
    // A dish on one restaurant's menu. Orders copy the name and price, so later edits do not change past orders.
    // Added through Restaurant.AddMenuItem.
    public class MenuItem : Entity, IAuditableEntity
    {
<<<<<<< Updated upstream
        public MenuItem(string name, string description, decimal price)
=======
        public MenuItem(string name, string description, decimal price, string imageUrl = null)
>>>>>>> Stashed changes
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(100);
            DomainValidation<string>.Ensure(description ?? string.Empty, nameof(description)).HasMaxLength(300);
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

        public string Category { get; private set; }

        public decimal Price { get; private set; }

<<<<<<< Updated upstream
        // Unavailable items cannot be ordered, but they stay on record
=======
        public string ImageUrl { get; private set; }

>>>>>>> Stashed changes
        public bool IsAvailable { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Restaurant Restaurant { get; private set; }

        public virtual MenuCategory MenuCategory { get; private set; }

<<<<<<< Updated upstream
        public void ChangePrice(decimal price)
        {
            EnsurePrice(price);

            Price = price;
        }
=======
        public void ChangeImage(string imageUrl) => ImageUrl = NormalizeImage(imageUrl);
>>>>>>> Stashed changes

        public void SetAvailability(bool available) => IsAvailable = available;

        // Called by MenuCategory when the item is added to it
        internal void SetCategoryName(string categoryName) => Category = categoryName;

<<<<<<< Updated upstream
=======
        private static string NormalizeImage(string imageUrl)
        {
            var value = (imageUrl ?? string.Empty).Trim();
            DomainValidation<string>.Ensure(value, nameof(imageUrl)).HasMaxLength(50);

            return value.Length == 0 ? null : value;
        }

>>>>>>> Stashed changes
        private static void EnsurePrice(decimal price) =>
            DomainValidation<decimal>.Ensure(price)
                .IsGreaterThan(0m, "Price must be greater than zero and at most 100000.")
                .IsLessThanOrEqualTo(100000m, "Price must be greater than zero and at most 100000.");
    }
}
