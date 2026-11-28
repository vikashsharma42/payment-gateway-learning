using DineBook.Application.Security;
using DineBook.Domain.Entities;
using DineBook.Domain.Enums;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    /// <summary>
    /// Creates the first admin account when no admin exists yet. Does nothing once an admin exists.
    /// The email and password come from configuration (Admin:Email and Admin:Password, for example
    /// in user-secrets). The password is stored only as a hash, produced by IPasswordHasher.
    /// </summary>
    public sealed class AdminAccountSeeder(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        IConfiguration configuration,
        ILogger<AdminAccountSeeder> logger)
    {
        public async Task SeedAsync(CancellationToken ct = default)
        {
            if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct))
                return;

            var email = ApplicationUser.NormalizeEmail(configuration["Admin:Email"]);
            var password = configuration["Admin:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("No admin account exists and Admin:Email or Admin:Password is not set.");
                return;
            }

            if (await db.Users.AnyAsync(u => u.Email == email, ct))
            {
                // The address already belongs to a non-admin account. Never change it silently.
                logger.LogWarning("Admin:Email belongs to an existing non-admin account; no admin was created.");
                return;
            }

            var admin = new ApplicationUser(
                email,
                hasher.Hash(password),
                "System",
                "Administrator",
                UserRole.Admin,
                countryCode: CountryRules.Default,
                phone: null,
                restaurantId: null);

            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("First admin account created.");
        }
    }
}