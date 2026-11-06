using DineBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DineBook.Infrastructure.Database.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable(nameof(Customer));

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
