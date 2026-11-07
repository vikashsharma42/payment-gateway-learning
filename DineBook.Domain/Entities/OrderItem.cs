using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using System;

namespace DineBook.Domain.Entities
{
    // One dish in an order, with the name, price and image copied at the time of ordering.
    // Added through Orders.AddOrderItem.
    public class OrderItem : Entity, IAuditableEntity
    {
        public OrderItem(long menuItemId, string itemName, decimal unitPrice, int quantity, string imageUrl = null)
        {
            DomainValidation<long>.Ensure(menuItemId, "Menu item").IsGreaterThan(0);
            DomainValidation<string>.Ensure(itemName, "Item name").IsNotNullOrWhiteSpace();
            DomainValidation<decimal>.Ensure(unitPrice, "Unit price").IsGreaterThan(0);
            DomainValidation<int>.Ensure(quantity).IsGreaterThanOrEqualTo(1);

            MenuItemId = menuItemId;
            ItemName = itemName.Trim();
            UnitPrice = unitPrice;
            Quantity = quantity;
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        }

        public long OrderId { get; private set; }

        public long MenuItemId { get; private set; }

        public string ItemName { get; private set; }

        public decimal UnitPrice { get; private set; }

        public int Quantity { get; private set; }

        // Copied from the menu, so the order history keeps the photo even if the dish is later removed
        public string ImageUrl { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Orders Order { get; private set; }

        public virtual MenuItem MenuItem { get; private set; }

        public decimal LineTotal() => UnitPrice * Quantity;
    }
}
