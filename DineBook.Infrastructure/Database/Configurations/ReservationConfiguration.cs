using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.ToTable(nameof(Reservation));
            builder.HasIndex(x => x.Code).IsUnique();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasOne(x => x.Restaurant)
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
               .WithMany()
               .HasForeignKey(x => x.ApplicationUserId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(nameof(Reservation.FoodOrders)).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(x => new { x.RestaurantId, x.StartsAt });

            builder.Ignore(x => x.EndsAt);
        }
    }
}
