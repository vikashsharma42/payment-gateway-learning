using System;
using System.Threading;
using System.Threading.Tasks;
using DineBook.Infrastructure.Database.Security;

namespace DineBook.Application.JWTSecurity
{
    /// <summary>
    /// Persists hashed refresh tokens in the database. Raw tokens are never stored.
    /// </summary>
    public interface IRefreshTokenStore
    {
        /// <summary>Stores a new active refresh token hash for the user.</summary>
        Task StoreAsync(string tokenHash, long appUserId, DateTimeOffset expiresAtUtc, CancellationToken ct = default);

        /// <summary>Returns the token row for the given hash, or null if it does not exist.</summary>
        Task<UserRefreshToken> FindAsync(string tokenHash, CancellationToken ct = default);

        /// <summary>
        /// Atomically revokes the old token (reason: Rotated) and stores its replacement.
        /// Returns false if the old token was not active. If the old token had already
        /// been rotated, the store also revokes all of the user's sessions (ReuseDetected).
        /// </summary>
        Task<bool> RotateAsync(string oldTokenHash, string newTokenHash, long appUserId, DateTimeOffset newExpiresAtUtc, CancellationToken ct = default);

        /// <summary>
        /// Revokes a single active token. Already-revoked tokens keep their original reason.
        /// Returns true if the token was found active and revoked, false otherwise.
        /// </summary>
        Task<bool> RevokeAsync(string tokenHash, RefreshRevokeReason reason, CancellationToken ct = default);

        /// <summary>Revokes all active tokens for a user. Already revoked tokens keep their original reason.</summary>
        Task RevokeAllForUserAsync(long appUserId, RefreshRevokeReason reason, CancellationToken ct = default);
    }
}
