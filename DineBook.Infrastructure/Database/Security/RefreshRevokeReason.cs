namespace DineBook.Infrastructure.Database.Security
{
    // Stored as text on UserRefreshTokens.RevokeReason
    public enum RefreshRevokeReason
    {
        Rotated,
        LoggedOut,
        SessionReplaced,
        AdminRevoked,
        ReuseDetected
    }
}
