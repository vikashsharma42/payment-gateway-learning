using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DineBook.Infrastructure.Database;
using DineBook.Infrastructure.Database.Security;

namespace DineBook.Application.JWTSecurity
{
    /// <summary>
    /// Database-backed refresh token store. Requires EF Core 7+ for ExecuteUpdateAsync.
    /// All state changes use conditional UPDATEs so concurrent requests cannot
    /// double-rotate or overwrite the original revoke reason.
    /// </summary>
    public sealed class DbRefreshTokenStore : IRefreshTokenStore
    {
        private readonly ApplicationDbContext _db;

        public DbRefreshTokenStore(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task StoreAsync(string tokenHash, long appUserId, DateTimeOffset expiresAtUtc, CancellationToken ct = default)
        {
            _db.UserRefreshTokens.Add(new UserRefreshToken(tokenHash, appUserId, expiresAtUtc));
            await _db.SaveChangesAsync(ct);
        }

        public Task<UserRefreshToken> FindAsync(string tokenHash, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                return Task.FromResult<UserRefreshToken>(null);

            return _db.UserRefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        }

        /// <summary>
        /// Atomically revokes the old token and stores the new one.
        /// The WHERE clause checks both RevokedAtUtc and ExpiresAtUtc so an expired
        /// token cannot be rotated even if the caller's pre-check raced with the clock.
        /// Returns false if the old token was not active. If it was revoked because it
        /// had already been rotated, that is treated as a replay and every session for
        /// the user is revoked.
        /// </summary>
        public async Task<bool> RotateAsync(string oldTokenHash, string newTokenHash, long appUserId, DateTimeOffset newExpiresAtUtc, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(oldTokenHash) || string.IsNullOrWhiteSpace(newTokenHash))
                return false;

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Clear tracked entities from a previous failed attempt so a retry starts clean.
                _db.ChangeTracker.Clear();

                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                var now = DateTimeOffset.UtcNow;

                // Check-and-set in one statement: only one concurrent caller can flip
                // RevokedAtUtc from null, and only while the token is not yet expired.
                var rotated = await _db.UserRefreshTokens
                    .Where(t => t.TokenHash == oldTokenHash
                             && t.RevokedAtUtc == null
                             && t.ExpiresAtUtc > now)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.RevokedAtUtc, now)
                        .SetProperty(t => t.RevokeReason, RefreshRevokeReason.Rotated)
                        .SetProperty(t => t.ReplacedByTokenHash, newTokenHash), ct);

                if (rotated == 0)
                {
                    await RevokeUserOnReuseAsync(oldTokenHash, ct);
                    await tx.CommitAsync(ct);
                    return false;
                }

                _db.UserRefreshTokens.Add(new UserRefreshToken(newTokenHash, appUserId, newExpiresAtUtc));
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return true;
            });
        }

        /// <summary>
        /// Revokes a single active token.
        /// Returns true if the token was found active and revoked; false otherwise.
        /// Already-revoked tokens keep their original reason.
        /// </summary>
        public async Task<bool> RevokeAsync(string tokenHash, RefreshRevokeReason reason, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                return false;

            var rows = await _db.UserRefreshTokens
                .Where(t => t.TokenHash == tokenHash && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.RevokedAtUtc, DateTimeOffset.UtcNow)
                    .SetProperty(t => t.RevokeReason, reason), ct);

            return rows > 0;
        }

        public async Task RevokeAllForUserAsync(long appUserId, RefreshRevokeReason reason, CancellationToken ct = default)
        {
            await _db.UserRefreshTokens
                .Where(t => t.ApplicationUserId == appUserId && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.RevokedAtUtc, DateTimeOffset.UtcNow)
                    .SetProperty(t => t.RevokeReason, reason), ct);
        }

        /// <summary>
        /// A token that was already rotated is being presented again. Someone
        /// else has probably copied it, so end every session for that user.
        /// Tokens revoked for other reasons (logout, session replaced) are not reuse.
        /// </summary>
        private async Task RevokeUserOnReuseAsync(string tokenHash, CancellationToken ct)
        {
            var existing = await _db.UserRefreshTokens
                .AsNoTracking()
                .Where(t => t.TokenHash == tokenHash)
                .Select(t => new { t.ApplicationUserId, t.RevokedAtUtc, t.RevokeReason })
                .FirstOrDefaultAsync(ct);

            if (existing is not null
                && existing.RevokedAtUtc is not null
                && existing.RevokeReason == RefreshRevokeReason.Rotated)
            {
                await RevokeAllForUserAsync(existing.ApplicationUserId, RefreshRevokeReason.ReuseDetected, ct);
            }
        }
    }
}
