using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable(nameof(ApplicationUser));
            builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(ApplicationUser.MaxEmailLength).IsRequired();
            builder.Property(x => x.FirstName).HasMaxLength(ApplicationUser.MaxNameLength).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(ApplicationUser.MaxNameLength).IsRequired();
            builder.Property(x => x.Phone).HasMaxLength(ApplicationUser.MaxPhoneLength);
            builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

            // Restaurant of a customer or staff member. Null for admins.
            builder.HasOne<Restaurant>()
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Emails are stored lower-case, so one account per address
            builder.HasIndex(x => x.Email).IsUnique();

            // Phone numbers are unique when present. Staff may have none.
            builder.HasIndex(x => x.Phone)
                .IsUnique()
                .HasFilter("[Phone] IS NOT NULL");

            // FullName is computed from the two name columns
            builder.Ignore(x => x.FullName);
        }
    }
}
