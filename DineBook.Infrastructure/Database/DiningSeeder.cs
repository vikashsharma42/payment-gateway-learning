using DineBook.Domain.Entities;
using DineBook.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Infrastructure.Database
{
    /// <summary>
    /// Adds sample restaurants, tables and menus when they are missing. Development only.
    /// </summary>
    public static class DiningSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
        {
            if (!await db.Restaurants.AnyAsync(ct))
            {
                var spiceRoute = new Restaurant("Spice Route", new TimeOnly(12, 0), new TimeOnly(23, 30));
                spiceRoute.AddLocation(new Location("Main",
                    Address.Create("12 Sample Street", "Sample City", "Sample State", "India", "110001"), "+911100000001"));
                spiceRoute.AddLocation(new Location("South",
                    Address.Create("8 Sample Road", "Sample City", "Sample State", "India", "110049"), "+911100000002"));

                var chai = new Restaurant("Chai & Co.", new TimeOnly(8, 0), new TimeOnly(22, 0));
                chai.AddLocation(new Location("Main",
                    Address.Create("45 Sample Lane", "Sample City", "Sample State", "India", "110016"), "+911100000003"));

                db.Restaurants.AddRange(spiceRoute, chai);
                await db.SaveChangesAsync(ct);

                // Locations have ids now, so tables can be placed at them
                var spice = await db.Restaurants.Include(r => r.Locations).SingleAsync(r => r.Name == "Spice Route", ct);
                var chaiCo = await db.Restaurants.Include(r => r.Locations).SingleAsync(r => r.Name == "Chai & Co.", ct);

                var main = spice.Locations.Single(l => l.Name == "Main").Id;
                var south = spice.Locations.Single(l => l.Name == "South").Id;
                var chaiMain = chaiCo.Locations.Single(l => l.Name == "Main").Id;

                spice.AddTable(main, new Table("T1", 2));
                spice.AddTable(main, new Table("T2", 2));
                spice.AddTable(main, new Table("T3", 4));
                spice.AddTable(south, new Table("T1", 6));

                chaiCo.AddTable(chaiMain, new Table("A1", 2));
                chaiCo.AddTable(chaiMain, new Table("A2", 4));

                await db.SaveChangesAsync(ct);
            }

            if (await db.MenuItems.AnyAsync(ct))
                return;

            var spiceMenu = await db.Restaurants.Include(r => r.MenuCategories).SingleAsync(r => r.Name == "Spice Route", ct);
            var chaiMenu = await db.Restaurants.Include(r => r.MenuCategories).SingleAsync(r => r.Name == "Chai & Co.", ct);

            await AddDishAsync(db, spiceMenu, "Dal Makhani", "Slow-cooked black lentils", "Mains", 320m, "dal-makhani", ct);
            await AddDishAsync(db, spiceMenu, "Paneer Tikka", "Char-grilled cottage cheese", "Starters", 280m, "paneer-tikka", ct);
            await AddDishAsync(db, spiceMenu, "Garlic Naan", "Tandoor-baked bread", "Breads", 60m, "garlic-naan", ct);
            await AddDishAsync(db, spiceMenu, "Sweet Lassi", "Chilled yoghurt drink", "Drinks", 120m, "sweet-lassi", ct);
            await AddDishAsync(db, chaiMenu, "Masala Chai", "Spiced milk tea", "Drinks", 40m, "masala-chai", ct);
            await AddDishAsync(db, chaiMenu, "Veg Sandwich", "Grilled vegetables on toast", "Snacks", 150m, "veg-sandwich", ct);
        }

        private static async Task AddDishAsync(
            ApplicationDbContext db, Restaurant restaurant, string name, string description,
            string categoryName, decimal price, string photoSeed, CancellationToken ct)
        {
            // The category must be saved first: a dish is filed under the category's id
            var category = restaurant.MenuCategories.FirstOrDefault(c => c.Name == categoryName);
            if (category is null)
            {
                category = new MenuCategory(categoryName);
                restaurant.AddMenuCategory(category);
                await db.SaveChangesAsync(ct);
            }

            var imageUrl = $"https://picsum.photos/seed/{photoSeed}/400/300";
            restaurant.AddMenuItem(category.Id, new MenuItem(name, description, price, imageUrl));
            await db.SaveChangesAsync(ct);
        }
    }
}
