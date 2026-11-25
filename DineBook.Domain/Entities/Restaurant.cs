using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace DineBook.Domain.Entities
{
    public class Restaurant : Entity, IAuditableEntity
    {
        public const decimal DefaultTaxPercent = 18.00m;
        public const decimal DefaultSystemAccessFee = 10.00m;
        public const decimal MaxTaxPercent = 28.00m;
        public const decimal MaxSystemAccessFee = 500.00m;

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

        public void AddMenuCategory(MenuCategory menuCategory)
        {
            DomainValidation<MenuCategory>.Ensure(menuCategory).IsNotNull();

            menuCategory.RestaurantId = Id;
            _menuCategories.Add(menuCategory);
        }

        public void AddReservation(long tableId, Reservation reservation)
        {
            DomainValidation<Reservation>.Ensure(reservation).IsNotNull();

            reservation.RestaurantId = Id;
            reservation.TableId = tableId;
            _reservations.Add(reservation);
        }

        public void AddTable(long locationId, Table table)
        {
            DomainValidation<Table>.Ensure(table).IsNotNull();

            table.RestaurantId = Id;
            table.LocationId = locationId;
            _tables.Add(table);
        }

        public void AddMenuItem(long menuCategoryId, MenuItem menuItem)
        {
            DomainValidation<MenuItem>.Ensure(menuItem).IsNotNull();

            menuItem.RestaurantId = Id;
            menuItem.MenuCategoryId = menuCategoryId;
            _menuItems.Add(menuItem);
        }
    }
}