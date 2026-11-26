using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;

namespace DineBook.Domain.Entities
{
    // One dish in an order, with the name and price copied at the time of ordering.
    // Added through Orders.AddOrderItem.
    public class OrderItem : Entity, IAuditableEntity
    {
        public OrderItem(string itemName, Money unitPrice, int quantity)
        {
            DomainValidation<string>.Ensure(itemName, "Item name").IsNotNullOrWhiteSpace();
            DomainValidation<Money>.Ensure(unitPrice, "Unit price").IsNotNull();
            DomainValidation<decimal>.Ensure(unitPrice.Amount, "Unit price").IsGreaterThan(0);
            DomainValidation<int>.Ensure(quantity).IsGreaterThanOrEqualTo(1);

            ItemName = itemName.Trim();
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public long OrderId { get; internal set; }

        public long MenuItemId { get; internal set; }

        public string ItemName { get; private set; }

        public Money UnitPrice { get; private set; }

        public int Quantity { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Orders Order { get; private set; }

        public virtual MenuItem MenuItem { get; private set; }

        public Money LineTotal() => UnitPrice.Multiply(Quantity);
    }
}
