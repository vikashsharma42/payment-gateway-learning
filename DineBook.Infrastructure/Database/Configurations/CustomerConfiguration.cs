using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DineBook.Domain.Entities.Customers;
using DineBook.Domain.Entities.Identity;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");
            builder.Property(x => x.Name).HasMaxLength(Customer.MaxNameLength).IsRequired();
            builder.Property(x => x.Phone).HasMaxLength(Customer.MaxPhoneDigits).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(Customer.MaxEmailLength).IsRequired();
            builder.Property(x => x.CreatedDateTime).IsRequired();
            builder.Property(x => x.CreatedBy).HasMaxLength(256);
            builder.Property(x => x.UpdatedDateTime).IsRequired();
            builder.Property(x => x.UpdatedBy).HasMaxLength(256);

            // One customer per phone number
            builder.HasIndex(x => x.Phone).IsUnique();

            builder.Navigation(nameof(Customer.Reservations)).UsePropertyAccessMode(PropertyAccessMode.Field);

            // Optional link to a sign-in account. A user is never deleted while a customer points at it.
            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
