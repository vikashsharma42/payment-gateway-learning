using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Entities.Menu;
using DineBook.Domain.Entities.Organization;
using DineBook.Domain.ValueObjects;

namespace DineBook.Infrastructure.Database
{
    /// <summary>
    /// Adds sample restaurants and menus when they are missing. Development only.
    /// </summary>
    public static class DiningSeeder
    {
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

            if (await db.MenuItems.AnyAsync(ct))
                return;

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

        private static void AddDish(Restaurant restaurant, string name, string description, string category, decimal price) =>
            restaurant.AddMenuItem(new MenuItem(name, description, price), restaurant.GetOrAddMenuCategory(category));
    }
}
