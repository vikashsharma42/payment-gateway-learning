namespace DineBook.Application.JWTSecurity
{
    /// <summary>Error codes returned when a refresh fails.</summary>
    public static class RefreshErrorCodes
    {
        public const string InvalidToken = "INVALID_REFRESH_TOKEN";
        public const string SessionReplaced = "SESSION_REPLACED";
        public const string SessionEnded = "SESSION_ENDED";
    }

    public sealed class RefreshResult
    {
        public JwtSignInResult Session { get; init; }

        public string ErrorCode { get; init; }

        public bool Succeeded => Session is not null;

        public static RefreshResult Ok(JwtSignInResult session) => new RefreshResult { Session = session };

        public static RefreshResult Fail(string errorCode) => new RefreshResult { ErrorCode = errorCode };
    }
}
