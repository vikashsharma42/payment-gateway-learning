using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
    {
        public void Configure(EntityTypeBuilder<SecurityEvent> builder)
        {
            builder.ToTable(nameof(SecurityEvent));
            builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40).IsRequired();

            // Risk score by address, and failed sign-ins by account
            builder.HasIndex(x => new { x.IpAddress, x.OccurredAt });
            builder.HasIndex(x => new { x.Kind, x.Subject, x.OccurredAt });
            builder.HasIndex(x => x.OccurredAt);
        }
    }
}
