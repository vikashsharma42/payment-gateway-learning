using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Entities.Organization;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
    {
        public void Configure(EntityTypeBuilder<RestaurantTable> builder)
        {
            builder.ToTable("RestaurantTables");
            builder.Property(x => x.Label).HasMaxLength(RestaurantTable.MaxLabelLength).IsRequired();
            builder.Property(x => x.Location).HasMaxLength(RestaurantTable.MaxLocationLength).IsRequired();
            builder.Property(x => x.Capacity).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // IsActive is computed from Status
            builder.Ignore(x => x.IsActive);
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

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
