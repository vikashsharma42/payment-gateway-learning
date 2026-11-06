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

            // FullName is computed from the two name columns
            builder.Ignore(x => x.FullName);

            // Emails are stored lower-case, so one account per address
            builder.HasIndex(x => x.Email).IsUnique();
        }
    }
}
