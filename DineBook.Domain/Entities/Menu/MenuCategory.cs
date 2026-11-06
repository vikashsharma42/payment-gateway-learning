using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using DineBook.Domain.Abstractions;
using DineBook.Domain.Entities.Organization;
using DineBook.Domain.Exceptions;

namespace DineBook.Domain.Entities.Menu
{
    /// <summary>
    /// A section of one restaurant's menu, such as "Starters". Added through <see cref="Restaurant.AddMenuCategory"/>.
    /// </summary>
    public class MenuCategory : BaseEntity, IAuditableEntity
    {
        public const int MaxNameLength = 50;

        public MenuCategory(string name)
        {
            DomainValidation<string>.Ensure(name, "Category").IsNotNullOrWhiteSpace().HasMaxLength(MaxNameLength);

            Name = name.Trim();
        }

        public long RestaurantId { get; private set; }

        public string Name { get; private set; }

        public DateTimeOffset CreatedDateTime { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset UpdatedDateTime { get; set; }

        public string UpdatedBy { get; set; }

        [ForeignKey(nameof(RestaurantId))]
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
