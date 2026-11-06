using System;

namespace DineBook.Infrastructure.Database.Security
{
    /// <summary>
    /// A hashed refresh token. The raw token is only ever held by the browser cookie.
    /// This is a persistence record, not a domain concept, so it lives with the database code.
    /// </summary>
    public class UserRefreshToken
    {
        public UserRefreshToken(string tokenHash, long applicationUserId, DateTimeOffset expiresAtUtc)
        {
            TokenHash = tokenHash;
            ApplicationUserId = applicationUserId;
            ExpiresAtUtc = expiresAtUtc;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public long Id { get; private set; }

        // SHA-256 of the raw token, as hex
        public string TokenHash { get; private set; }

        public long ApplicationUserId { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; }

        public DateTimeOffset ExpiresAtUtc { get; private set; }

        public DateTimeOffset? RevokedAtUtc { get; private set; }

        public RefreshRevokeReason? RevokeReason { get; private set; }

        public string ReplacedByTokenHash { get; private set; }
    }
}
