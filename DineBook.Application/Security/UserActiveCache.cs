using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DineBook.Infrastructure.Database;

namespace DineBook.Application.Security
{
    public sealed class UserActiveCache : IUserActiveCache
    {
        // Short, so a deactivated user is cut off quickly even if Invalidate was not called
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _db;

        public UserActiveCache(IMemoryCache cache, ApplicationDbContext db)
        {
            _cache = cache;
            _db = db;
        }

        public async Task<bool> IsActiveAsync(long userId, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(Key(userId), out bool cached))
                return cached;

            var active = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.IsActive, ct);

            _cache.Set(Key(userId), active, Lifetime);
            return active;
        }

        public void Invalidate(long userId) => _cache.Remove(Key(userId));

        private static string Key(long userId) => $"user:active:{userId}";
    }
}
