using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Identity;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("Users");
            builder.Property(x => x.Email).HasMaxLength(ApplicationUser.MaxEmailLength).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            builder.Property(x => x.FirstName).HasMaxLength(ApplicationUser.MaxNameLength).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(ApplicationUser.MaxNameLength).IsRequired();
            builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            // FullName is computed from the two name columns
            builder.Ignore(x => x.FullName);

            // Emails are stored lower-case, so one account per address
            builder.HasIndex(x => x.Email).IsUnique();
        }
    }
}
