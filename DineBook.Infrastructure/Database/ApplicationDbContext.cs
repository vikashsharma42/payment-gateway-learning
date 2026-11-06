using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DineBook.Domain.Abstractions;
using DineBook.Domain.Entities.Customers;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Entities.Identity;
using DineBook.Domain.Entities.Menu;
using DineBook.Domain.Entities.Ordering;
using DineBook.Domain.Entities.Organization;
using DineBook.Domain.Entities.Payments;
using DineBook.Infrastructure.Database.Security;

namespace DineBook.Infrastructure.Database
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserProvider currentUser = null) : DbContext(options)
    {
        private const string SystemUser = "system";

        // Identity and sessions
        public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

        public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();

        // Guests
        public DbSet<Customer> Customers => Set<Customer>();

        // Restaurants, tables and reservations
        public DbSet<Restaurant> Restaurants => Set<Restaurant>();

        public DbSet<RestaurantTable> RestaurantTables => Set<RestaurantTable>();

        public DbSet<Reservation> Reservations => Set<Reservation>();

        // Menu
        public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();

        public DbSet<MenuItem> MenuItems => Set<MenuItem>();

        // Food ordering
        public DbSet<Orders> Orders => Set<Orders>();

        public DbSet<OrderItems> OrderItems => Set<OrderItems>();

        // Bills and gateway payments
        public DbSet<Bill> Bills => Set<Bill>();

        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampTimestamps();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampTimestamps();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // Fills the audit fields of every IAuditableEntity, so the domain code does not need a clock or a user
        private void StampTimestamps()
        {
            var now = DateTimeOffset.UtcNow;
            var user = currentUser?.UserName ?? SystemUser;

            foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDateTime = now;
                    entry.Entity.CreatedBy = user;
                    entry.Entity.UpdatedDateTime = now;
                    entry.Entity.UpdatedBy = user;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDateTime = now;
                    entry.Entity.UpdatedBy = user;
                }
            }
        }
    }
}
