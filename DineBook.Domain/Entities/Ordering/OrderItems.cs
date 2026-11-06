using System;
using System.ComponentModel.DataAnnotations.Schema;
using DineBook.Domain.Abstractions;
using DineBook.Domain.Entities.Menu;
using DineBook.Domain.Exceptions;

namespace DineBook.Domain.Entities.Ordering
{
    /// <summary>
    /// One dish in an order, with the name and price copied at the time of ordering.
    /// Added through <see cref="Orders.AddOrderItem"/>.
    /// </summary>
    public class OrderItems : BaseEntity, IAuditableEntity
    {
        public OrderItems(long menuItemId, string itemName, decimal unitPrice, int quantity)
        {
            DomainValidation<long>.Ensure(menuItemId, "Menu item").IsGreaterThan(0, "Menu item is required.");
            DomainValidation<string>.Ensure(itemName, "Item name").IsNotNullOrWhiteSpace();
            DomainValidation<decimal>.Ensure(unitPrice, "Unit price").IsGreaterThan(0, "Unit price must be greater than zero.");
            DomainValidation<int>.Ensure(quantity).IsGreaterThanOrEqualTo(1, "Quantity must be at least 1.");

            MenuItemId = menuItemId;
            ItemName = itemName.Trim();
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public long OrderId { get; private set; }

        public long MenuItemId { get; private set; }

        public string ItemName { get; private set; }

        public decimal UnitPrice { get; private set; }

        public int Quantity { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Orders Order { get; private set; }

        [ForeignKey(nameof(MenuItemId))]
        public virtual MenuItem MenuItem { get; private set; }

        public decimal LineTotal() => UnitPrice * Quantity;
    }
}
