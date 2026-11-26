using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class LocationConfiguration : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> builder)
        {
            builder.ToTable(nameof(Location));

            // A location's web address is unique within its restaurant, not globally
            builder.HasIndex(x => new { x.RestaurantId, x.Slug }).IsUnique();

            builder.OwnsOne(x => x.Address, a => ValueObjectMappings.MapAddress(a));

            // Many locations belong to one restaurant. Deleting a restaurant removes its locations.
            builder.HasOne(x => x.Restaurant)
                 .WithMany(r => r.Locations)
                 .HasForeignKey(x => x.RestaurantId)
                 .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(nameof(Location.Tables))
              .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
