using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Menu;
using DineBook.Domain.Entities.Ordering;
using DineBook.Domain.Entities.Organization;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
    {
        public void Configure(EntityTypeBuilder<Restaurant> builder)
        {
            builder.ToTable("Restaurants");
            builder.Property(x => x.Name).HasMaxLength(Restaurant.MaxNameLength).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.Phone).HasMaxLength(Restaurant.MaxPhoneLength).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(Restaurant.MaxDescriptionLength).IsRequired();
            builder.Property(x => x.Cuisine).HasMaxLength(Restaurant.MaxCuisineLength).IsRequired();
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

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
