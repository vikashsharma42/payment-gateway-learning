using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
    {
        public void Configure(EntityTypeBuilder<RestaurantTable> builder)
        {
            builder.ToTable(nameof(RestaurantTable));
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Many tables belong to one restaurant. Deleting a restaurant removes its tables.
            builder.HasOne(x => x.Restaurant)
                .WithMany(r => r.Tables)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Removed tables are soft-deleted: hidden everywhere, but kept for past bookings
            builder.HasQueryFilter(x => !x.IsDeleted);

            // Labels are unique within one restaurant among active tables, so a removed label can be reused
            builder.HasIndex(x => new { x.RestaurantId, x.Label })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
