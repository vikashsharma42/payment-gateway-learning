using Microsoft.EntityFrameworkCore;
using ProductShop.Domain.Entities;

namespace ProductShop.Infrastructure.Database
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Products> Products => Set<Products>();

        public DbSet<Orders> Orders => Set<Orders>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}