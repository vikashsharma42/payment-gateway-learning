using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable(nameof(Payment));
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
