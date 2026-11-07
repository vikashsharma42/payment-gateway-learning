using DineBook.Domain.Abstractions;
using DineBook.Domain.Enums;
using DineBook.Domain.Validation;
using DineBook.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DineBook.Domain.Entities
{
    // A restaurant and the tables and menu it owns.
    public class Restaurant : Entity, IAuditableEntity
    {
        public const int MaxNameLength = 100;
        public const int MaxPhoneLength = 15;
        public const int MaxDescriptionLength = 500;
        public const int MaxCuisineLength = 50;
        public const decimal DefaultTaxPercent = 18.00m;
        public const decimal DefaultSystemAccessFee = 10.00m;
        public const decimal MaxTaxPercent = 28.00m;
        public const decimal MaxSystemAccessFee = 500.00m;

        // Required by EF Core: Address is an owned value object and cannot be bound through the constructor
        private Restaurant()
        {
        }

        public Restaurant(string name, Address address, TimeOnly opensAt, TimeOnly closesAt)
        {
            DomainValidation<string>.Ensure(name).IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);
            DomainValidation<Address>.Ensure(address).IsNotNull();
            DomainValidation<TimeOnly>.Ensure(closesAt).IsDateNewerThan(opensAt, "Closing time must be after opening time.");

            Name = name.Trim();
            Address = address;
            OpensAt = opensAt;
            ClosesAt = closesAt;
            Status = RestaurantStatus.Active;
            Phone = string.Empty;
            Description = string.Empty;
            Cuisine = string.Empty;
            TaxPercent = DefaultTaxPercent;
            SystemAccessFee = DefaultSystemAccessFee;
            TaxType = "GST";
        }

        public string Name { get; private set; }

        public Address Address { get; private set; }

        public TimeOnly OpensAt { get; private set; }

        public TimeOnly ClosesAt { get; private set; }

        public RestaurantStatus Status { get; private set; }

        public string Phone { get; private set; }

        public string Description { get; private set; }

        public string Cuisine { get; private set; }

        // GST rate applied to each bill (percent), and the flat system access fee added per food order
        public decimal TaxPercent { get; private set; }

        public decimal SystemAccessFee { get; private set; }

        // Label shown on the bill, for example GST or VAT
        public string TaxType { get; private set; }

        public void UpdateCharges(decimal taxPercent, decimal systemAccessFee)
        {
            DomainValidation<decimal>.Ensure(taxPercent, nameof(taxPercent))
                .IsBetween(0m, MaxTaxPercent, $"Tax must be between 0% and {MaxTaxPercent}%.");
            DomainValidation<decimal>.Ensure(systemAccessFee, nameof(systemAccessFee))
                .IsBetween(0m, MaxSystemAccessFee, $"System access fee must be between 0 and {MaxSystemAccessFee}.");

            TaxPercent = decimal.Round(taxPercent, 2, MidpointRounding.AwayFromZero);
            SystemAccessFee = decimal.Round(systemAccessFee, 2, MidpointRounding.AwayFromZero);
        }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        // Inactive restaurants take no new bookings. Bookings and orders already made are kept.
        public bool IsActive => Status == RestaurantStatus.Active;

        private readonly List<RestaurantTable> _tables = [];
        public IReadOnlyCollection<RestaurantTable> Tables => _tables.AsReadOnly();

        private readonly List<MenuCategory> _menuCategories = [];
        public IReadOnlyCollection<MenuCategory> MenuCategories => _menuCategories.AsReadOnly();

        private readonly List<MenuItem> _menuItems = [];
        public IReadOnlyCollection<MenuItem> MenuItems => _menuItems.AsReadOnly();

        public void Activate() => Status = RestaurantStatus.Active;

        public void Deactivate() => Status = RestaurantStatus.Inactive;

        public void UpdateDetails(string phone, string description, string cuisine)
        {
            DomainValidation<string>.Ensure(phone ?? string.Empty, nameof(phone)).HasMaxLength(MaxPhoneLength);
            DomainValidation<string>.Ensure(description ?? string.Empty, nameof(description)).HasMaxLength(MaxDescriptionLength);
            DomainValidation<string>.Ensure(cuisine ?? string.Empty, nameof(cuisine)).HasMaxLength(MaxCuisineLength);

            Phone = (phone ?? string.Empty).Trim();
            Description = (description ?? string.Empty).Trim();
            Cuisine = (cuisine ?? string.Empty).Trim();
        }

        // Compares times of day only. A booking that runs past midnight is not supported.
        public bool IsOpenFor(DateTime startsAt, int durationMinutes)
        {
            var start = startsAt.TimeOfDay;
            var end = start + TimeSpan.FromMinutes(durationMinutes);

            return start >= OpensAt.ToTimeSpan() && end <= ClosesAt.ToTimeSpan();
        }

        public void AddTable(RestaurantTable table)
        {
            DomainValidation<RestaurantTable>.Ensure(table).IsNotNull();
            EnsureLabelIsFree(table.Label, except: null);

            _tables.Add(table);
        }

        public RestaurantTable FindTable(long tableId)
        {
            var table = _tables.FirstOrDefault(t => t.Id == tableId && !t.IsDeleted);
            DomainValidation<RestaurantTable>.Ensure(table).IsNotNull("Table not found.");

            return table;
        }

        public void RenameTable(RestaurantTable table, string label)
        {
            DomainValidation<RestaurantTable>.Ensure(table).IsNotNull();
            EnsureOwns(table);
            DomainValidation<string>.Ensure(label).IsNotNullOrWhiteSpace().HasMaxLength(RestaurantTable.MaxLabelLength);
            EnsureLabelIsFree(label.Trim(), except: table);

            table.Rename(label);
        }

        public void RemoveTable(RestaurantTable table)
        {
            DomainValidation<RestaurantTable>.Ensure(table).IsNotNull();
            EnsureOwns(table);

            table.Delete();
        }

        public MenuCategory FindMenuCategory(string name) =>
            _menuCategories.FirstOrDefault(c => string.Equals(c.Name, (name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));

        // Reuses the category with this name (ignoring case), or adds a new one
        public MenuCategory GetOrAddMenuCategory(string name)
        {
            var category = FindMenuCategory(name);
            if (category is not null)
                return category;

            category = new MenuCategory(name);
            AddMenuCategory(category);
            return category;
        }

        public void AddMenuCategory(MenuCategory category)
        {
            DomainValidation<MenuCategory>.Ensure(category).IsNotNull();

            DomainValidation<MenuCategory>.Ensure(FindMenuCategory(category.Name), nameof(category))
                .IsNull("This category already exists.");

            _menuCategories.Add(category);
        }

        public void AddMenuItem(MenuItem menuItem, MenuCategory category)
        {
            DomainValidation<MenuItem>.Ensure(menuItem).IsNotNull();
            DomainValidation<MenuCategory>.Ensure(category).IsNotNull();

            DomainValidation<bool>.Ensure(_menuCategories.Contains(category)).IsTrue("Category not found.");

            _menuItems.Add(menuItem);
            category.AddMenuItem(menuItem);
        }

        private void EnsureOwns(RestaurantTable table)
        {
            DomainValidation<bool>.Ensure(_tables.Contains(table)).IsTrue("Table not found.");
        }

        // Labels are unique among active tables, ignoring case. A table may keep its own label.
        private void EnsureLabelIsFree(string label, RestaurantTable except)
        {
            var taken = _tables.Any(t =>
                !t.IsDeleted
                && !ReferenceEquals(t, except)
                && string.Equals(t.Label, label.Trim(), StringComparison.OrdinalIgnoreCase));

            DomainValidation<bool>.Ensure(taken).IsFalse("A table with this label already exists.");
        }
    }
}
