using DineBook.Domain.Entities;
using DineBook.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Infrastructure.Database
{
    /// <summary>
    /// Adds sample restaurants and menus when they are missing, and fills in dish photos. Development only.
    /// </summary>
    public static class DiningSeeder
    {
        // Placeholder photos. Replace them with real dish photos before going live.
        private static readonly Dictionary<string, string> DishImages = new()
        {
            ["Dal Makhani"] = "https://picsum.photos/seed/dinebook-dal-makhani/400/300",
            ["Paneer Tikka"] = "https://picsum.photos/seed/dinebook-paneer-tikka/400/300",
            ["Garlic Naan"] = "https://picsum.photos/seed/dinebook-garlic-naan/400/300",
            ["Sweet Lassi"] = "https://picsum.photos/seed/dinebook-sweet-lassi/400/300",
            ["Masala Chai"] = "https://picsum.photos/seed/dinebook-masala-chai/400/300",
            ["Veg Sandwich"] = "https://picsum.photos/seed/dinebook-veg-sandwich/400/300",
        };

        public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
        {
            if (!await db.Restaurants.AnyAsync(ct))
            {
                var bistro = new Restaurant(
                    "Spice Route",
                    Address.Create("12 Sample Street", "Sample City", "Sample State", "110001"),
                    new TimeOnly(12, 0),
                    new TimeOnly(23, 30));
                bistro.AddTable(new RestaurantTable("T1", 2, "Window"));
                bistro.AddTable(new RestaurantTable("T2", 2, string.Empty));
                bistro.AddTable(new RestaurantTable("T3", 4, "Patio"));
                bistro.AddTable(new RestaurantTable("T4", 6, string.Empty));

                var cafe = new Restaurant(
                    "Chai & Co.",
                    Address.Create("45 Sample Lane", "Sample City", "Sample State", "110016"),
                    new TimeOnly(8, 0),
                    new TimeOnly(22, 0));
                cafe.AddTable(new RestaurantTable("A1", 2, string.Empty));
                cafe.AddTable(new RestaurantTable("A2", 4, string.Empty));

                db.Restaurants.AddRange(bistro, cafe);
                await db.SaveChangesAsync(ct);
            }

            if (!await db.MenuItems.AnyAsync(ct))
            {
                var spice = await db.Restaurants.Include(r => r.MenuCategories).SingleAsync(r => r.Name == "Spice Route", ct);
                var chai = await db.Restaurants.Include(r => r.MenuCategories).SingleAsync(r => r.Name == "Chai & Co.", ct);

                AddDish(spice, "Dal Makhani", "Slow-cooked black lentils", "Mains", 320m);
                AddDish(spice, "Paneer Tikka", "Char-grilled cottage cheese", "Starters", 280m);
                AddDish(spice, "Garlic Naan", "Tandoor-baked bread", "Breads", 60m);
                AddDish(spice, "Sweet Lassi", "Chilled yoghurt drink", "Drinks", 120m);
                AddDish(chai, "Masala Chai", "Spiced milk tea", "Drinks", 40m);
                AddDish(chai, "Veg Sandwich", "Grilled vegetables on toast", "Snacks", 150m);

                await db.SaveChangesAsync(ct);
            }

            // Databases seeded before dish photos existed get them here
            var withoutPhoto = await db.MenuItems.Where(m => m.ImageUrl == null).ToListAsync(ct);
            foreach (var item in withoutPhoto)
            {
                if (DishImages.TryGetValue(item.Name, out var url))
                    item.ChangeImage(url);
            }

            if (withoutPhoto.Count > 0)
                await db.SaveChangesAsync(ct);
        }

        private static void AddDish(Restaurant restaurant, string name, string description, string category, decimal price) =>
            restaurant.AddMenuItem(
                new MenuItem(name, description, price, DishImages.GetValueOrDefault(name)),
                restaurant.GetOrAddMenuCategory(category));
    }
}
