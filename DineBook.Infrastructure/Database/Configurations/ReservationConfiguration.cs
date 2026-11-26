using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.ToTable(nameof(Reservation));
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Many bookings belong to one restaurant
            builder.HasOne(x => x.Restaurant)
                .WithMany(r => r.Reservations)
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            // The guest is a customer account. Name, phone and email are read from it.
            builder.HasOne(x => x.Customer)
               .WithMany()
               .HasForeignKey(x => x.ApplicationUserId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(nameof(Reservation.FoodOrders)).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(x => new { x.RestaurantId, x.StartsAt });

            // Computed from other columns, not stored
            builder.Ignore(x => x.EndsAt);
            builder.Ignore(x => x.CustomerName);
            builder.Ignore(x => x.CustomerPhone);
            builder.Ignore(x => x.CustomerEmail);
        }
    }
}
