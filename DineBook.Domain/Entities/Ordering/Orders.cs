using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DineBook.Domain.Abstractions;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Entities.Menu;
using DineBook.Domain.Entities.Organization;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;

namespace DineBook.Domain.Entities.Ordering
{
    /// <summary>
    /// Food ordered for one reservation (a restaurant order). One order can hold many items.
    /// Added through <see cref="Reservation.AddFoodOrder"/>.
    /// </summary>
    public class Orders : AggregateRoot, IAuditableEntity
    {
        public const int MaxItems = 30;
        public const int MaxQuantityPerItem = 20;

        public Orders(long restaurantId)
        {
            DomainValidation<long>.Ensure(restaurantId, "Restaurant").IsGreaterThan(0, "Restaurant is required.");

            RestaurantId = restaurantId;
            Status = OrderStatus.Placed;
        }

        public long ReservationId { get; private set; }

        public long RestaurantId { get; private set; }

        public OrderStatus Status { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        [ForeignKey(nameof(ReservationId))]
        public virtual Reservation Reservation { get; private set; }

        [ForeignKey(nameof(RestaurantId))]
        public virtual Restaurant Restaurant { get; private set; }

        private readonly List<OrderItems> _items = [];
        public IReadOnlyCollection<OrderItems> Items => _items.AsReadOnly();

        public decimal Total() => _items.Sum(i => i.LineTotal());

        /// <summary>
        /// Adds a dish. It must be available and on the menu of this order's restaurant. Each dish appears at most once,
        /// with 1 to 20 units. The name and price are copied from the menu item.
        /// </summary>
        public void AddOrderItem(MenuItem menuItem, int quantity)
        {
            DomainValidation<MenuItem>.Ensure(menuItem, "Item").IsNotNull("One of the items was not found.");

            DomainValidation<long>.Ensure(menuItem.RestaurantId, nameof(menuItem))
                .IsEqualTo(RestaurantId, "This item is not on the menu of this restaurant.");
            DomainValidation<bool>.Ensure(menuItem.IsAvailable).IsTrue($"{menuItem.Name} is not available right now.");
            DomainValidation<bool>.Ensure(_items.Any(i => i.MenuItemId == menuItem.Id))
                .IsFalse("Each item can appear only once in an order.");
            DomainValidation<int>.Ensure(quantity)
                .IsBetween(1, MaxQuantityPerItem, $"Quantity for {menuItem.Name} must be between 1 and {MaxQuantityPerItem}.");
            DomainValidation<int>.Ensure(_items.Count)
                .IsLessThanOrEqualTo(MaxItems - 1, $"An order can have at most {MaxItems} different items.");

            _items.Add(new OrderItems(menuItem.Id, menuItem.Name, menuItem.Price, quantity));
        }

        public void MarkServed() => MoveFrom(OrderStatus.Placed, OrderStatus.Served, "Only placed orders can be served.");

        public void Cancel() => MoveFrom(OrderStatus.Placed, OrderStatus.Cancelled, "Only placed orders can be cancelled.");

        // Called by Reservation.AddFoodOrder
        internal void AssignReservation(long reservationId) => ReservationId = reservationId;

        private void MoveFrom(OrderStatus expected, OrderStatus next, string message)
        {
            DomainValidation<OrderStatus>.Ensure(Status).IsEqualTo(expected, message);

            Status = next;
        }
    }
}
