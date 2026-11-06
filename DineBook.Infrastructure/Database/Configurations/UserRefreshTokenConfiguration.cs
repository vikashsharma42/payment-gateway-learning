using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Identity;
using DineBook.Infrastructure.Database.Security;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
    {
        public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
        {
            builder.ToTable("UserRefreshTokens");
            builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
            builder.Property(x => x.RevokeReason).HasConversion<string>().HasMaxLength(20);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.ExpiresAtUtc).IsRequired();

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => x.ApplicationUserId);

            // Deleting a user removes their sessions
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
