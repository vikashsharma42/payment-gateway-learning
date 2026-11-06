using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using DineBook.Application.JWTSecurity;
using DineBook.Application.Security;

namespace DineBook.Web.Infrastructure
{
    /// <summary>
    /// Configures the JWT bearer handler for a Razor Pages site. Browsers do not send an Authorization header
    /// on page navigation, so the token travels in an HttpOnly cookie and is handed to the handler here.
    /// When the access cookie has expired, the refresh cookie is used to start a new one.
    /// </summary>
    public sealed class JwtBearerSetup : IConfigureNamedOptions<JwtBearerOptions>
    {
        private const string LoginPath = "/Admin/Login";

        // Requests that arrive together with the same refresh token must share one rotation.
        // A second rotation of the same token would look like token theft and end the session.
        private static readonly SemaphoreSlim RefreshLock = new SemaphoreSlim(1, 1);
        private static readonly TimeSpan RefreshShare = TimeSpan.FromSeconds(15);

        private readonly JwtOptions _jwt;

        public JwtBearerSetup(IOptions<JwtOptions> jwt)
        {
            _jwt = jwt.Value;
        }

        public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);

        public void Configure(string name, JwtBearerOptions options)
        {
            if (name != JwtBearerDefaults.AuthenticationScheme)
                return;

            options.RequireHttpsMetadata = true;
            options.SaveToken = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                ValidateIssuer = true,
                ValidIssuer = _jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = ClaimTypes.Email,
                RoleClaimType = ClaimTypes.Role
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ReadTokenFromCookieAsync,
                OnTokenValidated = CheckSessionStillAllowedAsync,
                OnChallenge = RedirectToLogin,
                OnForbidden = RedirectToLoginDenied
            };
        }

        private static async Task ReadTokenFromCookieAsync(MessageReceivedContext context)
        {
            var cookies = context.Request.Cookies;

            if (cookies.TryGetValue(AuthCookies.AccessName, out var access) && !string.IsNullOrWhiteSpace(access))
            {
                context.Token = access;
                return;
            }

            if (!cookies.TryGetValue(AuthCookies.RefreshName, out var refresh) || string.IsNullOrWhiteSpace(refresh))
                return;

            var session = await RefreshAsync(context.HttpContext, refresh);
            if (session is null)
            {
                // The session ended (logged out, replaced by a newer sign-in, expired or revoked)
                AuthCookies.Clear(context.Response);
                return;
            }

            AuthCookies.Set(context.Response, session);
            context.Token = session.AccessToken;
        }

        private static async Task<JwtSignInResult> RefreshAsync(HttpContext http, string refreshToken)
        {
            var cache = http.RequestServices.GetRequiredService<IMemoryCache>();
            var cacheKey = "auth:refresh:" + refreshToken;

            await RefreshLock.WaitAsync(http.RequestAborted);
            try
            {
                if (cache.TryGetValue(cacheKey, out JwtSignInResult shared))
                    return shared;

                var manager = http.RequestServices.GetRequiredService<IJwtSignInManager>();
                var result = await manager.RefreshAsync(refreshToken);

                if (!result.Succeeded)
                    return null;

                cache.Set(cacheKey, result.Session, RefreshShare);
                return result.Session;
            }
            finally
            {
                RefreshLock.Release();
            }
        }

        // A signed token is not enough: it must not be blacklisted, and the user must still be active
        private static async Task CheckSessionStillAllowedAsync(TokenValidatedContext context)
        {
            var principal = context.Principal;
            var services = context.HttpContext.RequestServices;

            var jti = principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (services.GetRequiredService<IJwtTokenService>().IsTokenBlacklisted(jti))
            {
                context.Fail("This session has ended.");
                return;
            }

            var idClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(idClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
            {
                context.Fail("The token has no valid user.");
                return;
            }

            var active = await services.GetRequiredService<IUserActiveCache>()
                .IsActiveAsync(userId, context.HttpContext.RequestAborted);

            if (!active)
                context.Fail("This account is no longer active.");
        }

        // Not signed in: send people to the login page and bring them back afterwards.
        // Only GET navigation is redirected. Anything else gets a plain 401.
        private static Task RedirectToLogin(JwtBearerChallengeContext context)
        {
            context.HandleResponse();

            var request = context.Request;
            if (HttpMethods.IsGet(request.Method))
            {
                var returnUrl = Uri.EscapeDataString($"{request.PathBase}{request.Path}{request.QueryString}");
                context.Response.Redirect($"{LoginPath}?returnUrl={returnUrl}");
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }

            return Task.CompletedTask;
        }

        // Signed in, but the role is not enough
        private static Task RedirectToLoginDenied(ForbiddenContext context)
        {
            context.Response.Redirect($"{LoginPath}?denied=1");
            return Task.CompletedTask;
        }
    }
}
