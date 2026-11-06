using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Ordering;
using DineBook.Domain.Entities.Organization;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class OrdersConfiguration : IEntityTypeConfiguration<Orders>
    {
        public void Configure(EntityTypeBuilder<Orders> builder)
        {
            builder.ToTable("Orders");
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

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
