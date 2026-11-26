using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class TableConfiguration : IEntityTypeConfiguration<Table>
    {
        public void Configure(EntityTypeBuilder<Table> builder)
        {
            builder.ToTable(nameof(Table));
            builder.Property(x => x.Label).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Location).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.Ignore(x => x.IsActive);

            builder.HasOne(x => x.Restaurant)
                .WithMany(r => r.Tables)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // A table is at one location. Deleting a location is blocked while it has tables.
            builder.HasOne<Location>()
                .WithMany(l => l.Tables)
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
