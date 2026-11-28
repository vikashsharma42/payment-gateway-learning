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

            // Amount is a Money value object, stored as Amount_Amount and Amount_Currency
            builder.OwnsOne(x => x.Amount, a => ValueObjectMappings.MapMoney(a));
            builder.Property(x => x.AdvanceApplied).HasPrecision(18, 2);
            builder.Property(x => x.RefundedAmount).HasPrecision(18, 2);

            // A booking deposit has no food order, so the booking is linked directly
            builder.HasOne<Reservation>()
                .WithMany()
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

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
