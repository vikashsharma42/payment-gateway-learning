using DineBook.Application.Services.Interface;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Application.Services
{
    public sealed class CustomerContactService : ICustomerContactService
    {
        private readonly ApplicationDbContext _db;

        public CustomerContactService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CustomerContact> GetForUserAsync(long applicationUserId, CancellationToken ct = default)
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == applicationUserId, ct);

            if (user is null)
                return null;

            var phone = await _db.Customers.AsNoTracking()
                .Where(c => c.ApplicationUserId == applicationUserId)
                .Select(c => c.Phone)
                .FirstOrDefaultAsync(ct);

            return new CustomerContact(user.FullName, phone ?? string.Empty, user.Email);
        }
    }
}