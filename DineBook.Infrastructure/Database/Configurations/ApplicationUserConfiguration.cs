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

            // ISO 3166-1 alpha-2 country code, for example IN
            builder.Property(x => x.CountryCode).HasConversion<string>().HasMaxLength(2).IsRequired();

            // Restaurant of a customer or staff member. Null for admins.
            builder.HasOne<Restaurant>()
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Emails are stored lower-case. One account per email within a restaurant, so the same
            // address can be used at two restaurants. Admins have no restaurant (NULL). SQL Server
            // treats NULLs as equal in a unique index, so admin emails stay unique across the platform.
            builder.HasIndex(x => new { x.RestaurantId, x.Email }).IsUnique();

            // Phone is unique per restaurant when present. Staff may have none.
            builder.HasIndex(x => new { x.RestaurantId, x.Phone })
                .IsUnique()
                .HasFilter("[Phone] IS NOT NULL");

            // FullName is computed from the two name columns
            builder.Ignore(x => x.FullName);
        }
    }
}
