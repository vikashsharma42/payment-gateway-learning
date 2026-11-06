using DineBook.Domain.Entities;
using DineBook.Infrastructure.Database.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
    {
        public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
        {
            builder.ToTable(nameof(UserRefreshToken));
            builder.Property(x => x.RevokeReason).HasConversion<string>().HasMaxLength(20);

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
