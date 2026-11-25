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

            builder.Property(x => x.Name).HasMaxLength(Location.MaxNameLength).IsRequired();
            builder.Property(x => x.Phone).HasMaxLength(Location.MaxPhoneLength).IsRequired();

            builder.OwnsOne(x => x.Address, a => ValueObjectMappings.MapAddress(a));

            // Many locations belong to one restaurant. Deleting a restaurant removes its locations.
            builder.HasOne<Restaurant>()
                .WithMany(r => r.Locations)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
