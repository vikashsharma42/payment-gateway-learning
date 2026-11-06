using System;
using Microsoft.AspNetCore.Http;
using DineBook.Application.JWTSecurity;

namespace DineBook.Web.Infrastructure
{
    /// <summary>
    /// The two cookies that carry a session. Both are HttpOnly, so page scripts never see the tokens.
    /// The access cookie expires together with its token, so a missing access cookie means "refresh or sign in".
    /// </summary>
    public static class AuthCookies
    {
        public const string AccessName = "dinebook.access";
        public const string RefreshName = "dinebook.refresh";

        public static void Set(HttpResponse response, JwtSignInResult session)
        {
            response.Cookies.Append(AccessName, session.AccessToken, Options(session.AccessTokenExpiry));
            response.Cookies.Append(RefreshName, session.RefreshToken, Options(session.RefreshTokenExpiry));
        }

        public static void Clear(HttpResponse response)
        {
            response.Cookies.Delete(AccessName, Options(null));
            response.Cookies.Delete(RefreshName, Options(null));
        }

        private static CookieOptions Options(DateTimeOffset? expires) => new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Path = "/",
            Expires = expires
        };
    }
}
