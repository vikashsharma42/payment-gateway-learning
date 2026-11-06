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
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.OwnsOne(x => x.Address, a => ValueObjectMappings.MapAddress(a));

            // The relations themselves are defined on the dependent side (RestaurantTable, MenuCategory, MenuItem, Reservation, Orders)
            builder.Navigation(nameof(Restaurant.Tables)).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(nameof(Restaurant.MenuItems)).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(nameof(Restaurant.MenuCategories)).UsePropertyAccessMode(PropertyAccessMode.Field);

            // IsActive is computed from Status
            builder.Ignore(x => x.IsActive);
        }
    }
}
