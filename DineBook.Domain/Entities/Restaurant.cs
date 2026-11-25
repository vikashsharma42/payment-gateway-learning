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
<<<<<<< Updated upstream
        public const int MaxNameLength = 100;
        public const int MaxPhoneLength = 15;
        public const int MaxDescriptionLength = 500;
        public const int MaxCuisineLength = 50;
=======
        public const decimal DefaultTaxPercent = 18.00m;
        public const decimal DefaultSystemAccessFee = 10.00m;
        public const decimal MaxTaxPercent = 28.00m;
        public const decimal MaxSystemAccessFee = 500.00m;
>>>>>>> Stashed changes

        // A restaurant starts with no locations. Add at least one with AddLocation before it takes bookings.
        public Restaurant(string name, TimeOnly opensAt, TimeOnly closesAt)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(100);
            DomainValidation<TimeOnly>.Ensure(closesAt).IsDateNewerThan(opensAt, "Closing time must be after opening time.");

            Name = name.Trim();
<<<<<<< Updated upstream
            Address = address;
            OpensAt = opensAt;
            ClosesAt = closesAt;
            Status = RestaurantStatus.Active;
            Phone = string.Empty;
            Description = string.Empty;
            Cuisine = string.Empty;
=======
            Slug = RestaurantSlug.FromName(Name);
            OpensAt = opensAt;
            ClosesAt = closesAt;
            Status = RestaurantStatus.Active;
            TaxPercent = DefaultTaxPercent;
            SystemAccessFee = DefaultSystemAccessFee;
            TaxType = "GST";
>>>>>>> Stashed changes
        }

        public string Name { get; private set; }

<<<<<<< Updated upstream
        public Address Address { get; private set; }
=======
        public string Slug { get; private set; }
>>>>>>> Stashed changes

        public TimeOnly OpensAt { get; private set; }

        public TimeOnly ClosesAt { get; private set; }

        public RestaurantStatus Status { get; private set; }

        public string Phone { get; private set; }

        public string Description { get; private set; }

        public string Cuisine { get; private set; }

<<<<<<< Updated upstream
=======
        // GST rate applied to each bill (percent), and the flat system access fee added per food order
        public decimal TaxPercent { get; private set; }

        public decimal SystemAccessFee { get; private set; }

        // Label shown on the bill, for example GST or VAT
        public string TaxType { get; private set; }

>>>>>>> Stashed changes
        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        // Inactive restaurants take no new bookings. Bookings and orders already made are kept.
        //public bool IsActive => Status == RestaurantStatus.Active;

        private readonly List<Location> _locations = [];
        public virtual IReadOnlyCollection<Location> Locations => _locations.AsReadOnly();

        // Locations that staff can switch to
        // public IEnumerable<Location> ActiveLocations => _locations.Where(l => l.IsActive);

        private readonly List<Table> _tables = [];
        public virtual IReadOnlyCollection<Table> Tables => _tables.AsReadOnly();

        private readonly List<MenuCategory> _menuCategories = [];
        public virtual IReadOnlyCollection<MenuCategory> MenuCategories => _menuCategories.AsReadOnly();

        private readonly List<MenuItem> _menuItems = [];
        public virtual IReadOnlyCollection<MenuItem> MenuItems => _menuItems.AsReadOnly();

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
            _locations.Add(location);
        }

        public void AddTable(long locationId, Table table)
        {
            DomainValidation<Table>.Ensure(table).IsNotNull();
            _tables.Add(table);
        }

        public void AddMenuItem(MenuItem menuItem)
        {
            DomainValidation<MenuItem>.Ensure(menuItem).IsNotNull();
            _menuItems.Add(menuItem);
        }
    }
}