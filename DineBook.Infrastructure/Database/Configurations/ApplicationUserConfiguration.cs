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

            // Staff and managers belong to one restaurant. Customers and admins have none.
            builder.HasOne<Restaurant>()
                .WithMany()
                .HasForeignKey(x => x.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            // FullName is computed from the two name columns
            builder.Ignore(x => x.FullName);

            // Emails are stored lower-case, so one account per address
            builder.HasIndex(x => x.Email).IsUnique();
        }
    }
}
