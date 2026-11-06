using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Ordering;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class OrderItemsConfiguration : IEntityTypeConfiguration<OrderItems>
    {
        public void Configure(EntityTypeBuilder<OrderItems> builder)
        {
            builder.ToTable("OrderItems");
            builder.Property(x => x.ItemName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.UnitPrice).HasPrecision(10, 2);
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            // An item belongs to one order. Deleting the order removes its items.
            builder.HasOne(x => x.Order)
                .WithMany(nameof(Orders.Items))
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // An item refers to a menu item. Menu items are never deleted while they have orders.
            builder.HasOne(x => x.MenuItem)
                .WithMany()
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
