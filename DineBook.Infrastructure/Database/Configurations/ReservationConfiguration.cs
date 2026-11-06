using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Dining;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.ToTable("Reservations");

            builder.Property(x => x.Code).HasMaxLength(Reservation.CodeLength).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.CustomerPhone).HasMaxLength(15).IsRequired();
            builder.Property(x => x.CustomerEmail).HasMaxLength(254).IsRequired();
            builder.Property(x => x.SpecialRequests).HasMaxLength(Reservation.MaxSpecialRequestsLength).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            // Restrict, not cascade: a restaurant and its tables both lead here, and SQL Server
            // rejects two cascade paths to the same row. Reservations are kept for history.
            builder.HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional link to the guest record. Customers are kept for history.
            builder.HasOne(x => x.Customer)
                .WithMany(c => c.Reservations)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(nameof(Reservation.FoodOrders)).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(x => new { x.RestaurantId, x.StartsAt });

            builder.Ignore(x => x.EndsAt);
        }
    }
}
