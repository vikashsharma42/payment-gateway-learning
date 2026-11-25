using DineBook.Domain.Entities;
using DineBook.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

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
                var bistro = new Restaurant("Spice Route", new TimeOnly(12, 0), new TimeOnly(23, 30));
                bistro.AddLocation(new Location("Main",
                    Address.Create("12 Sample Street", "Sample City", "Sample State", "India", "110001"), "+911100000001"));
                bistro.AddLocation(new Location("South", Address.Create("8 Sample Road", "Sample City", "Sample State", "India", "110049"), "+911100000002"));

                var cafe = new Restaurant("Chai & Co.", new TimeOnly(8, 0), new TimeOnly(22, 0));
                cafe.AddLocation(new Location("Main", Address.Create("45 Sample Lane", "Sample City", "Sample State", "India", "110016"), "+911100000003"));

                db.Restaurants.AddRange(bistro, cafe);
                await db.SaveChangesAsync(ct);

                // Locations have ids now, so tables can be placed at them
                var spiceRoute = await db.Restaurants.Include(r => r.Locations).SingleAsync(r => r.Name == "Spice Route", ct);
                var chai = await db.Restaurants.Include(r => r.Locations).SingleAsync(r => r.Name == "Chai & Co.", ct);
                var main = spiceRoute.Locations.Single(l => l.Name == "Main").Id;
                var south = spiceRoute.Locations.Single(l => l.Name == "South").Id;
                var chaiMain = chai.Locations.Single(l => l.Name == "Main").Id;

                spiceRoute.AddTable(main, new Table("T1", 2, "Window"));
                spiceRoute.AddTable(main, new Table("T2", 2, string.Empty));
                spiceRoute.AddTable(main, new Table("T3", 4, "Patio"));
                spiceRoute.AddTable(south, new Table("T1", 6, string.Empty));

                chai.AddTable(chaiMain, new Table("A1", 2, string.Empty));
                chai.AddTable(chaiMain, new Table("A2", 4, string.Empty));

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
