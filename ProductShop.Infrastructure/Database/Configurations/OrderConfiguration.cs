using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductShop.Domain.Entities;

namespace ProductShop.Infrastructure.Database.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Orders>
    {
        public void Configure(EntityTypeBuilder<Orders> builder)
        {
            builder.ToTable(nameof(Orders));
            builder.Property(x => x.GatewayOrderId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.PaymentSessionId).HasMaxLength(200);
            builder.Property(x => x.IdempotencyKey).HasMaxLength(64);
            builder.Property(x => x.CustomerEmail).HasMaxLength(254);
            builder.Property(x => x.RefundId).HasMaxLength(64);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            // Fast webhook/status lookup, and duplicate Cashfree orders are rejected by the database
            builder.HasIndex(x => x.GatewayOrderId).IsUnique();

            // Same idempotency key always maps to the same order. Filtered: old rows have no key.
            builder.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
        }
    }
}
