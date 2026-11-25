using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class RestaurantTableConfiguration : IEntityTypeConfiguration<Table>
    {
        public void Configure(EntityTypeBuilder<Table> builder)
        {
            builder.ToTable(nameof(Table));
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Many tables belong to one restaurant. Deleting a restaurant removes its tables.
            builder.HasOne(x => x.Restaurant)
                .WithMany(r => r.Tables)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Every table is at one location. Restrict: a location with tables cannot be deleted.
            builder.HasOne<Location>()
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Removed tables are soft-deleted: hidden everywhere, but kept for past bookings
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Labels are unique within one location among active tables, so a removed label can be reused
            builder.HasIndex(x => new { x.LocationId, x.Label })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
