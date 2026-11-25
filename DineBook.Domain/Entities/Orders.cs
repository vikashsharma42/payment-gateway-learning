using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DineBook.Domain.Entities
{
    // Food ordered for one reservation (a restaurant order). One order can hold many items.
    // Added through Reservation.AddFoodOrder.
    public class Orders : Entity, IAuditableEntity
    {
        public Orders()
        {
            Status = OrderStatus.Placed;
        }

        public long ReservationId { get; internal set; }

        public long RestaurantId { get; internal set; }

        public OrderStatus Status { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Reservation Reservation { get; private set; }

        public virtual Restaurant Restaurant { get; private set; }

        private readonly List<OrderItem> _items = [];
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        private readonly List<Payment> _payments = [];
        public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();


        public decimal Total() => _items.Sum(i => i.LineTotal());

        // True once a payment for this order has succeeded. Needs Payments to be loaded.
        public bool IsPaid => _payments.Any(p => p.Status == PaymentStatus.Paid);

        // The dish must be available and on this order's restaurant menu. Each dish appears at most once,
        // with 1 to 20 units. The name and price are copied from the menu item.
        public void AddOrderItem(MenuItem menuItem, int quantity)
        {
            DomainValidation<MenuItem>.Ensure(menuItem, "Item").IsNotNull();

            DomainValidation<long>.Ensure(menuItem.RestaurantId, nameof(menuItem)).IsEqualTo(RestaurantId);
            DomainValidation<bool>.Ensure(menuItem.IsAvailable).IsTrue($"{menuItem.Name} is not available right now.");
            DomainValidation<bool>.Ensure(_items.Any(i => i.MenuItemId == menuItem.Id)).IsFalse("Each item can appear only once in an order.");
            DomainValidation<int>.Ensure(quantity).IsBetween(1, 20, $"Quantity for {menuItem.Name} must be between 1 and 20.");
            DomainValidation<int>.Ensure(_items.Count).IsLessThanOrEqualTo(29, $"An order can have at most 30 different items.");

            var item = new OrderItem(menuItem.Name, menuItem.Price, quantity);
            item.MenuItemId = menuItem.Id;
            item.OrderId = Id;
            _items.Add(item);
        }

        public void MarkServed() => MoveFrom(OrderStatus.Placed, OrderStatus.Served, "Only placed orders can be served.");

        public void Cancel()
        {
            DomainValidation<bool>.Ensure(IsPaid).IsFalse("A paid order cannot be cancelled.");

            MoveFrom(OrderStatus.Placed, OrderStatus.Cancelled, "Only placed orders can be cancelled.");
        }

        private void MoveFrom(OrderStatus expected, OrderStatus next, string message)
        {
            DomainValidation<OrderStatus>.Ensure(Status).IsEqualTo(expected, message);

            Status = next;
        }
    }
}
