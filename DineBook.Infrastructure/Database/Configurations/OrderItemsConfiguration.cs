using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class OrderItemsConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable(nameof(OrderItem));
            builder.Property(x => x.ItemName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ImageUrl).HasMaxLength(50);

            // UnitPrice is a Money value object, stored as UnitPrice_Amount and UnitPrice_Currency
            builder.OwnsOne(x => x.UnitPrice, a => ValueObjectMappings.MapMoney(a));

            // An item belongs to one order. Deleting the order removes its items.
            builder.HasOne(x => x.Order)
                .WithMany(o => o.Items)
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
