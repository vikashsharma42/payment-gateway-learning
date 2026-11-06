using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class OrdersConfiguration : IEntityTypeConfiguration<Orders>
    {
        public void Configure(EntityTypeBuilder<Orders> builder)
        {
            builder.ToTable(nameof(Orders));
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // Many orders belong to one restaurant. Restrict: restaurants with orders are never deleted.
            builder.HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many orders belong to one reservation. Deleting a reservation removes its orders.
            builder.HasOne(x => x.Reservation)
                .WithMany(r => r.FoodOrders)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(nameof(Orders.Items)).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
