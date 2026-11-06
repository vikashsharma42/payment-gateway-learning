using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DineBook.Application.Configuration;
using DineBook.Domain.Entities.Identity;
using DineBook.Domain.Enums;
using DineBook.Infrastructure.Database;

namespace DineBook.Application.Security
{
    /// <summary>
    /// Creates the first admin from the Admin configuration section when no admin exists yet.
    /// Does nothing when the section is empty or an admin is already there.
    /// </summary>
    public sealed class AdminAccountSeeder
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly AdminAccountOptions _options;

        public AdminAccountSeeder(ApplicationDbContext db, IPasswordHasher hasher, IOptions<AdminAccountOptions> options)
        {
            _db = db;
            _hasher = hasher;
            _options = options.Value;
        }

        public async Task SeedAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Email) || string.IsNullOrWhiteSpace(_options.Password))
                return;

            if (await _db.Users.AnyAsync(u => u.Role == UserRole.Admin, ct))
                return;

            var admin = new ApplicationUser(
                _options.Email,
                _hasher.Hash(_options.Password),
                _options.FirstName,
                _options.LastName,
                UserRole.Admin);

            _db.Users.Add(admin);
            await _db.SaveChangesAsync(ct);
        }
    }
}
