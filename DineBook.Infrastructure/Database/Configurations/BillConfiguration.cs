using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Dining;
using DineBook.Domain.Entities.Organization;
using DineBook.Domain.Entities.Payments;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class BillConfiguration : IEntityTypeConfiguration<Bill>
    {
        public void Configure(EntityTypeBuilder<Bill> builder)
        {
            builder.ToTable("Bills");
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            // Bills are financial records: never removed with a reservation or restaurant
            builder.HasOne(x => x.Reservation)
                .WithMany()
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ReservationId);
        }
    }
}
