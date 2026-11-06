using DineBook.Domain.Abstractions;
using DineBook.Domain.Validation;
using System;
using System.Collections.Generic;

namespace DineBook.Domain.Entities
{
    // A section of one restaurant's menu, such as "Starters". Added through Restaurant.AddMenuCategory.
    public class MenuCategory : Entity, IAuditableEntity
    {
        public MenuCategory(string name)
        {
            DomainValidation<string>.Ensure(name, "Category").IsNotNullOrWhiteSpace().HasMaxLength(50);

            Name = name.Trim();
        }

        public long RestaurantId { get; private set; }

        public string Name { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        public virtual Restaurant Restaurant { get; private set; }

        private readonly List<MenuItem> _menuItems = [];
        public IReadOnlyCollection<MenuItem> MenuItems => _menuItems.AsReadOnly();

        public void AddMenuItem(MenuItem menuItem)
        {
            DomainValidation<MenuItem>.Ensure(menuItem).IsNotNull();

            menuItem.SetCategoryName(Name);
            _menuItems.Add(menuItem);
        }
    }
}
