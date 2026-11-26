using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
    {
        public void Configure(EntityTypeBuilder<Restaurant> builder)
        {
            builder.ToTable(nameof(Restaurant));
            builder.Property(x => x.TaxPercent).HasPrecision(5, 2);
            builder.Property(x => x.SystemAccessFee).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Restaurant web address is unique
            builder.HasIndex(x => x.Slug).IsUnique();

            // The relations themselves are defined on the dependent side (Location, Table, MenuCategory, MenuItem, Reservation, Orders)
            builder.Navigation(nameof(Restaurant.Locations)).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(nameof(Restaurant.Tables)).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(nameof(Restaurant.MenuItems)).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(nameof(Restaurant.MenuCategories)).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(nameof(Restaurant.Reservations)).UsePropertyAccessMode(PropertyAccessMode.Field);

            // Computed from Status and the location list, not stored
            builder.Ignore(x => x.IsActive);
            builder.Ignore(x => x.ActiveLocations);
        }
    }
}
