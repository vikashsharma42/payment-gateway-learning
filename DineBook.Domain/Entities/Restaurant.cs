using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Exceptions;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DineBook.Domain.Entities
{
    public class Restaurant : Entity, IAuditableEntity
    {
        public const decimal DefaultTaxPercent = 18.00m;
        public const decimal DefaultSystemAccessFee = 10.00m;
        public const decimal MaxTaxPercent = 28.00m;
        public const decimal MaxSystemAccessFee = 500.00m;
        public const int MaxPhoneLength = 15;
        public const int MaxDescriptionLength = 500;

        // A restaurant starts with no locations. Add at least one with AddLocation before it takes bookings.
        public Restaurant(string name, TimeOnly opensAt, TimeOnly closesAt)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(100);
            DomainValidation<TimeOnly>.Ensure(closesAt).IsDateNewerThan(opensAt, "Closing time must be after opening time.");

            Name = name.Trim();
            Slug = RestaurantSlug.FromName(Name);
            OpensAt = opensAt;
            ClosesAt = closesAt;
            Status = RestaurantStatus.Active;
            TaxPercent = DefaultTaxPercent;
            SystemAccessFee = DefaultSystemAccessFee;
            TaxType = "GST";
        }

        public string Name { get; private set; }

        public string Slug { get; private set; }

        public TimeOnly OpensAt { get; private set; }

        public TimeOnly ClosesAt { get; private set; }

        public RestaurantStatus Status { get; private set; }

        public string Phone { get; private set; }

        public string Description { get; private set; }

        // GST rate applied to each bill (percent), and the flat system access fee added per food order
        public decimal TaxPercent { get; private set; }

        public decimal SystemAccessFee { get; private set; }

        // Label shown on the bill, for example GST or VAT
        public string TaxType { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public bool IsActive => Status == RestaurantStatus.Active;

        private readonly List<Location> _locations = [];
        public virtual IReadOnlyCollection<Location> Locations => _locations.AsReadOnly();

        private readonly List<Table> _tables = [];
        public virtual IReadOnlyCollection<Table> Tables => _tables.AsReadOnly();

        private readonly List<MenuCategory> _menuCategories = [];
        public virtual IReadOnlyCollection<MenuCategory> MenuCategories => _menuCategories.AsReadOnly();

        private readonly List<MenuItem> _menuItems = [];
        public virtual IReadOnlyCollection<MenuItem> MenuItems => _menuItems.AsReadOnly();

        private readonly List<Reservation> _reservations = [];
        public virtual IReadOnlyCollection<Reservation> Reservations => _reservations.AsReadOnly();

        public void UpdateDetails(string phone, string description)
        {
            var trimmedPhone = (phone ?? string.Empty).Trim();
            var trimmedDescription = (description ?? string.Empty).Trim();

            DomainValidation<string>.Ensure(trimmedPhone, nameof(phone)).HasMaxLength(MaxPhoneLength);
            DomainValidation<string>.Ensure(trimmedDescription, nameof(description)).HasMaxLength(MaxDescriptionLength);

            Phone = trimmedPhone;
            Description = trimmedDescription;
        }

        // Used when the name's slug is taken. The caller picks the free slug (for example "spice-garden-2").
        public void UseSlug(string slug)
        {
            DomainValidation<bool>.Ensure(RestaurantSlug.IsValid(slug)).IsTrue("The restaurant URL is not valid.");

            Slug = slug;
        }

        public void UpdateCharges(decimal taxPercent, decimal systemAccessFee)
        {
            DomainValidation<decimal>.Ensure(taxPercent, nameof(taxPercent)).IsBetween(0m, MaxTaxPercent, $"Tax must be between 0% and {MaxTaxPercent}%.");
            DomainValidation<decimal>.Ensure(systemAccessFee, nameof(systemAccessFee)).IsBetween(0m, MaxSystemAccessFee, $"System access fee must be between 0 and {MaxSystemAccessFee}.");

            TaxPercent = decimal.Round(taxPercent, 2, MidpointRounding.AwayFromZero);
            SystemAccessFee = decimal.Round(systemAccessFee, 2, MidpointRounding.AwayFromZero);
        }

        // Compares times of day only. A booking that runs past midnight is not supported.
        public bool IsOpenFor(DateTime startsAt, int durationMinutes)
        {
            var start = startsAt.TimeOfDay;
            var end = start + TimeSpan.FromMinutes(durationMinutes);

            return start >= OpensAt.ToTimeSpan() && end <= ClosesAt.ToTimeSpan();
        }

        public void AddLocation(Location location)
        {
            DomainValidation<Location>.Ensure(location).IsNotNull();

            location.RestaurantId = Id;
            _locations.Add(location);
        }

        // Opens or closes a location. The last open location cannot be closed.
        public void SetLocationActive(long locationId, bool active)
        {
            var location = _locations.FirstOrDefault(l => l.Id == locationId)
                ?? throw new DomainException("Location not found.");

            if (active)
            {
                location.Activate();
                return;
            }

            if (location.IsActive)
            {
                DomainValidation<int>.Ensure(_locations.Count(l => l.IsActive)).IsGreaterThan(1, "The last open location cannot be closed.");
            }

            location.Deactivate();
        }

        public void AddMenuCategory(MenuCategory menuCategory)
        {
            DomainValidation<MenuCategory>.Ensure(menuCategory).IsNotNull();

            menuCategory.RestaurantId = Id;
            _menuCategories.Add(menuCategory);
        }

        // Adds a table to one of this restaurant's locations. Labels are unique per location.
        public void AddTable(long locationId, Table table)
        {
            DomainValidation<Table>.Ensure(table).IsNotNull();

            var location = _locations.FirstOrDefault(l => l.Id == locationId)
                ?? throw new DomainException("Location not found.");

            DomainValidation<bool>.Ensure(
                _tables.Any(t => t.LocationId == location.Id && string.Equals(t.Label, table.Label, StringComparison.OrdinalIgnoreCase)))
                .IsFalse("A table with this label already exists at this location.");

            table.RestaurantId = Id;
            table.AssignLocation(location);
            _tables.Add(table);
            location.AddTable(table);
        }

        // The item goes into one of this restaurant's categories, which also sets the item's category name.
        public void AddMenuItem(long menuCategoryId, MenuItem menuItem)
        {
            DomainValidation<MenuItem>.Ensure(menuItem).IsNotNull();

            var category = _menuCategories.FirstOrDefault(c => c.Id == menuCategoryId)
                ?? throw new DomainException("Menu category not found.");

            menuItem.RestaurantId = Id;
            menuItem.MenuCategoryId = category.Id;
            category.AddMenuItem(menuItem);
            _menuItems.Add(menuItem);
        }

        // The table must belong to this restaurant, and the customer must be registered with this restaurant
        public void AddReservation(long tableId, Reservation reservation)
        {
            DomainValidation<Reservation>.Ensure(reservation).IsNotNull();

            var table = _tables.FirstOrDefault(t => t.Id == tableId)
                ?? throw new DomainException("Table not found.");
            DomainValidation<bool>.Ensure(table.IsActive).IsTrue("This table is out of service.");
            DomainValidation<bool>.Ensure(reservation.Customer?.RestaurantId == Id)
                .IsTrue("You can only book at restaurants you are registered with.");

            reservation.RestaurantId = Id;
            reservation.TableId = tableId;
            _reservations.Add(reservation);
        }
    }
}
