using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductShop.Domain.Orders;

namespace ProductShop.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.GatewayOrderId).HasMaxLength(100).IsRequired();
        builder.HasIndex(o => o.GatewayOrderId).IsUnique(); // fast webhook lookup, duplicate-safe
        builder.Property(o => o.Amount).HasPrecision(18, 2);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
    }
}
