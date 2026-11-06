using System;

namespace DineBook.Application.JWTSecurity
{
    public sealed class JwtSignInResult
    {
        public string AccessToken { get; init; } = string.Empty;

        public DateTimeOffset AccessTokenExpiry { get; init; }

        public string RefreshToken { get; init; } = string.Empty;

        public DateTimeOffset RefreshTokenExpiry { get; init; }
    }
}
