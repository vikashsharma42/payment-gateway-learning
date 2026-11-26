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

        // Used by EF Core when loading. Validation runs only in the public constructor.
        private Restaurant()
        {
        }

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

        // Locations that are open for staff and new bookings
        public IEnumerable<Location> ActiveLocations => _locations.Where(l => l.IsActive);

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

            DomainValidation<string>.Ensure(trimmedPhone, nameof(phone)).HasMaxLength(15);
            DomainValidation<string>.Ensure(trimmedDescription, nameof(description)).HasMaxLength(500);

            Phone = trimmedPhone;
            Description = trimmedDescription;
        }

        // Used when the name's slug is taken. The caller picks the free slug (for example "spice-garden-2").
        public void UseSlug(string slug)
        {
            DomainValidation<bool>.Ensure(RestaurantSlug.IsValid(slug)).IsTrue("The restaurant URL is not valid.");

            Slug = slug;
        }

        // Bill for an order's items: GST on the items, plus the access fee once per order
        public Bill CalculateBill(Money subtotal)
        {
            DomainValidation<Money>.Ensure(subtotal, "Subtotal").IsNotNull();
            var tax = Money.Create(
                decimal.Round(subtotal.Amount * TaxPercent / 100m, 2, MidpointRounding.AwayFromZero),
                subtotal.Currency);
            var accessFee = Money.Create(SystemAccessFee, subtotal.Currency);
            return new Bill(subtotal, tax, accessFee);
        }

        public void Activate() => Status = RestaurantStatus.Active;

        public void Deactivate() => Status = RestaurantStatus.Inactive;

        public void Rename(string name)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(100);
            Name = name.Trim();
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

        // The default location is the one a restaurant's web address opens with. The first location added is the default.
        public Location DefaultLocation => _locations.FirstOrDefault(l => l.IsDefault);

        public void AddLocation(Location location)
        {
            DomainValidation<Location>.Ensure(location).IsNotNull();

            location.RestaurantId = Id;
            location.SetSlug(UniqueLocationSlug(location.Slug, excludeLocationId: null));
            _locations.Add(location);

            if (DefaultLocation is null)
                location.SetDefault(true);
        }

        // Location slugs are unique within one restaurant, so /{restaurant}/{location} is unambiguous
        public void ChangeLocationSlug(long locationId, string slug)
        {
            var location = FindLocation(locationId);
            DomainValidation<bool>.Ensure(RestaurantSlug.IsValid(slug)).IsTrue("The location URL is not valid.");
            DomainValidation<bool>.Ensure(
                _locations.Any(l => l.Id != locationId && string.Equals(l.Slug, slug, StringComparison.Ordinal)) == false)
                .IsTrue("Another location of this restaurant already uses this URL.");
            location.SetSlug(slug);
        }

        public void SetDefaultLocation(long locationId)
        {
            var location = FindLocation(locationId);
            DomainValidation<bool>.Ensure(location.IsActive).IsTrue("Open the location before making it the default.");

            foreach (var other in _locations)
                other.SetDefault(other.Id == location.Id);
        }

        // Opens or closes a location. The last open location cannot be closed, and the default cannot be closed.
        public void SetLocationActive(long locationId, bool active)
        {
            var location = FindLocation(locationId);

            if (active)
            {
                location.Activate();
                return;
            }

            if (location.IsActive)
            {
                DomainValidation<int>.Ensure(_locations.Count(l => l.IsActive)).IsGreaterThan(1, "The last open location cannot be closed.");
                DomainValidation<bool>.Ensure(location.IsDefault).IsFalse("Make another location the default before closing this one.");
            }

            location.Deactivate();
        }

        private Location FindLocation(long locationId) =>
            _locations.FirstOrDefault(l => l.Id == locationId)
            ?? throw new DomainException("Location not found.");

        private string UniqueLocationSlug(string baseSlug, long? excludeLocationId)
        {
            var candidate = baseSlug;
            var number = 2;

            while (_locations.Any(l => l.Id != excludeLocationId && string.Equals(l.Slug, candidate, StringComparison.Ordinal)))
            {
                candidate = RestaurantSlug.WithSuffix(baseSlug, number);
                number++;
            }

            return candidate;
        }

        public void AddMenuCategory(MenuCategory menuCategory)
        {
            DomainValidation<MenuCategory>.Ensure(menuCategory).IsNotNull();

            menuCategory.RestaurantId = Id;
            _menuCategories.Add(menuCategory);
        }

        // Tables at one location, including the ones that are out of service (the staff page shows their status)
        public IReadOnlyList<Table> TablesAt(long locationId) =>
            _tables.Where(t => t.LocationId == locationId).ToList();

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
        public void MoveMenuItem(long menuItemId, long menuCategoryId)
        {
            var item = _menuItems.FirstOrDefault(i => i.Id == menuItemId)
                ?? throw new DomainException("Menu item not found.");
            var target = _menuCategories.FirstOrDefault(c => c.Id == menuCategoryId)
                ?? throw new DomainException("Menu category not found.");
            DomainValidation<bool>.Ensure(item.IsDeleted).IsFalse("A removed menu item cannot be moved.");

            var current = _menuCategories.FirstOrDefault(c => c.Id == item.MenuCategoryId);
            current?.RemoveMenuItem(item);

            item.MenuCategoryId = target.Id;
            target.AddMenuItem(item);
        }

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
