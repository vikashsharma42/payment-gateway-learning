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
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.GatewayOrderId).HasMaxLength(100).IsRequired();
            builder.Property(x => x.PaymentSessionId).HasMaxLength(200).IsRequired();
            builder.Property(x => x.IdempotencyKey).HasMaxLength(64).IsRequired();
            builder.Property(x => x.CustomerEmail).HasMaxLength(254).IsRequired();
            builder.Property(x => x.RefundId).HasMaxLength(100);

            // Amount is a Money value object, stored as Amount_Amount and Amount_Currency
            builder.OwnsOne(x => x.Amount, a => ValueObjectMappings.MapMoney(a));

            // Many payment attempts can belong to one order. Restrict: a paid order is never deleted with its payments.
            builder.HasOne(x => x.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Fast webhook/status lookup, and duplicate Cashfree orders are rejected by the database
            builder.HasIndex(x => x.GatewayOrderId).IsUnique();

            // Same idempotency key always maps to the same order. Filtered: old rows have no key.
            builder.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
        }
    }
}
