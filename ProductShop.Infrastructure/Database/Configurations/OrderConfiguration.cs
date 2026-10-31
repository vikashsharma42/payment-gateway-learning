using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductShop.Domain.Entities;

namespace ProductShop.Infrastructure.Database.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable(nameof(Order));
            builder.Property(x => x.GatewayOrderId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            // Fast webhook/status lookup, and duplicate orders are rejected by the database
            builder.HasIndex(x => x.GatewayOrderId).IsUnique();
        }
    }
}