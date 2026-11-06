using DineBook.Domain.Abstractions;
using DineBook.Domain.Entities;
using DineBook.Infrastructure.Database.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Infrastructure.Database
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserProvider currentUser = null) : DbContext(options)
    {
        private const string SystemUser = "system";

        // Identity and sessions
        public DbSet<ApplicationUser> Users { get; set; }

        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

        // Guests
        public DbSet<Customer> Customers { get; set; }

        // Restaurants, tables and reservations
        public DbSet<Restaurant> Restaurants { get; set; }

        public DbSet<RestaurantTable> RestaurantTables { get; set; }

        public DbSet<Reservation> Reservations { get; set; }

        // Menu
        public DbSet<MenuCategory> MenuCategories { get; set; }

        public DbSet<MenuItem> MenuItems { get; set; }

        // Food ordering
        public DbSet<Orders> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<Payment> Payments { get; set; }

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
