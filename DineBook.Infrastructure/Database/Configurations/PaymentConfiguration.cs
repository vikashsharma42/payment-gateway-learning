using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Payments;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.Property(x => x.GatewayOrderId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.PaymentSessionId).HasMaxLength(200);
            builder.Property(x => x.IdempotencyKey).HasMaxLength(64);
            builder.Property(x => x.CustomerEmail).HasMaxLength(254);
            builder.Property(x => x.RefundId).HasMaxLength(64);
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);
            builder.HasIndex(x => x.BillId);

            // A payment belongs to one bill. Bills are kept, so payments are never cascaded away.
            builder.HasOne(x => x.Bill)
                .WithMany()
                .HasForeignKey(x => x.BillId)
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
