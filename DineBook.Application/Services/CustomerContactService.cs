using DineBook.Application.Services.Interface;
using DineBook.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
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

            return user is null ? null : new CustomerContact(user.FullName, user.Phone, user.Email);
        }
    }
}
