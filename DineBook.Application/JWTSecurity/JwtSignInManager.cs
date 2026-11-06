using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DineBook.Application.Security;
using DineBook.Domain.Identity;
using DineBook.Infrastructure.Database;
using DineBook.Infrastructure.Database.Security;

namespace DineBook.Application.JWTSecurity
{
    /// <summary>
    /// Manages JWT-based sign-in, token refresh, and session revocation.
    ///
    /// Single-session rule: signing in revokes all existing sessions for the user
    /// (reason: SessionReplaced) so only the newest sign-in stays active. Clients
    /// that held an older session will receive SESSION_REPLACED on their next refresh.
    ///
    /// Deactivation / ban: call <see cref="RevokeAllSessionsAsync"/> and then
    /// <see cref="IUserActiveCache.Invalidate"/> so that the JWT bearer handler
    /// rejects the user's still-valid access token on their very next request.
    /// </summary>
    public class JwtSignInManager : IJwtSignInManager
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ApplicationDbContext _db;
        private readonly JwtOptions _jwtOptions;

        public JwtSignInManager(
            IJwtTokenService jwtTokenService,
            IRefreshTokenStore refreshTokenStore,
            IHttpContextAccessor httpContextAccessor,
            IPasswordHasher passwordHasher,
            ApplicationDbContext db,
            IOptions<JwtOptions> jwtOptions)
        {
            ArgumentNullException.ThrowIfNull(jwtTokenService);
            ArgumentNullException.ThrowIfNull(refreshTokenStore);
            ArgumentNullException.ThrowIfNull(jwtOptions);

            _jwtTokenService = jwtTokenService;
            _refreshTokenStore = refreshTokenStore;
            _httpContextAccessor = httpContextAccessor;
            _passwordHasher = passwordHasher;
            _db = db;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<JwtSignInResult> SignInAsync(ApplicationUser appUser, string password)
        {
            ArgumentNullException.ThrowIfNull(appUser);

            // Verify first, then check the account, so a deactivated account takes as long to reject as a wrong password
            var passwordMatches = _passwordHasher.Verify(password, appUser.PasswordHash);

            // A deactivated account cannot sign in, even with the right password
            if (!passwordMatches || !appUser.IsActive)
                return null;

            return await IssueSessionAsync(appUser);
        }

        public async Task<JwtSignInResult> SignInExternalAsync(ApplicationUser appUser)
        {
            ArgumentNullException.ThrowIfNull(appUser);
            return await IssueSessionAsync(appUser);
        }

        public async Task<RefreshResult> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return RefreshResult.Fail(RefreshErrorCodes.InvalidToken);

            var tokenHash = HashToken(refreshToken);
            var record = await _refreshTokenStore.FindAsync(tokenHash);

            if (record is null)
                return RefreshResult.Fail(RefreshErrorCodes.InvalidToken);

            if (record.RevokedAtUtc is not null)
            {
                switch (record.RevokeReason)
                {
                    case RefreshRevokeReason.SessionReplaced:
                        return RefreshResult.Fail(RefreshErrorCodes.SessionReplaced);

                    case RefreshRevokeReason.LoggedOut:
                    case RefreshRevokeReason.AdminRevoked:
                        return RefreshResult.Fail(RefreshErrorCodes.SessionEnded);

                    default:
                        await _refreshTokenStore.RevokeAllForUserAsync(record.ApplicationUserId, RefreshRevokeReason.ReuseDetected);
                        return RefreshResult.Fail(RefreshErrorCodes.InvalidToken);
                }
            }

            if (DateTimeOffset.UtcNow >= record.ExpiresAtUtc)
                return RefreshResult.Fail(RefreshErrorCodes.InvalidToken);

            var appUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == record.ApplicationUserId);

            // User deleted directly from DB, or account no longer exists.
            if (appUser is null)
                return RefreshResult.Fail(RefreshErrorCodes.SessionEnded);

            // User exists but has been banned or soft-deleted since this token was issued.
            if (!appUser.IsActive)
            {
                await _refreshTokenStore.RevokeAllForUserAsync(appUser.Id, RefreshRevokeReason.AdminRevoked);
                return RefreshResult.Fail(RefreshErrorCodes.SessionEnded);
            }

            var (accessToken, accessExpiry) = await GenerateAccessTokenAsync(appUser);

            var newRefreshToken = GenerateRefreshTokenValue();
            var refreshExpiry = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);

            var rotated = await _refreshTokenStore.RotateAsync(
                tokenHash,
                HashToken(newRefreshToken),
                appUser.Id,
                refreshExpiry);

            if (!rotated)
                return RefreshResult.Fail(RefreshErrorCodes.InvalidToken);

            return RefreshResult.Ok(new JwtSignInResult
            {
                AccessToken = accessToken,
                AccessTokenExpiry = accessExpiry,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiry = refreshExpiry
            });
        }

        public async Task<bool> LogoutAsync(string refreshToken, bool globalLogout = false)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            if (globalLogout)
            {
                var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (long.TryParse(userIdClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
                    await _refreshTokenStore.RevokeAllForUserAsync(userId, RefreshRevokeReason.LoggedOut);
            }
            else if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _refreshTokenStore.RevokeAsync(HashToken(refreshToken), RefreshRevokeReason.LoggedOut);
            }

            return true;
        }

        /// <summary>
        /// Revokes all sessions for a user. Call this when an admin deletes or bans a user.
        /// </summary>
        public async Task<bool> RevokeAllSessionsAsync(long userId, CancellationToken cancellationToken = default)
        {
            await _refreshTokenStore.RevokeAllForUserAsync(userId, RefreshRevokeReason.AdminRevoked, cancellationToken);
            return true;
        }

        private async Task<JwtSignInResult> IssueSessionAsync(ApplicationUser appUser)
        {
            await _refreshTokenStore.RevokeAllForUserAsync(appUser.Id, RefreshRevokeReason.SessionReplaced);

            var (accessToken, accessExpiry) = await GenerateAccessTokenAsync(appUser);

            var refreshToken = GenerateRefreshTokenValue();
            var refreshExpiry = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);
            await _refreshTokenStore.StoreAsync(HashToken(refreshToken), appUser.Id, refreshExpiry);

            return new JwtSignInResult
            {
                AccessToken = accessToken,
                AccessTokenExpiry = accessExpiry,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshExpiry
            };
        }

        private async Task<(string AccessToken, DateTimeOffset AccessExpiry)> GenerateAccessTokenAsync(ApplicationUser appUser)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, appUser.Id.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Email, appUser.Email),
                new(ClaimTypes.GivenName, appUser.FirstName),
                new(ClaimTypes.Surname, appUser.LastName),
                new(ClaimTypes.Role, appUser.Role.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var accessToken = await _jwtTokenService.GenerateJwtTokenAsync(claims);
            var accessExpiry = DateTimeOffset.UtcNow.Add(_jwtOptions.TokenExpiry);

            return (accessToken, accessExpiry);
        }

        private static string GenerateRefreshTokenValue()
        {
            Span<byte> bytes = stackalloc byte[64];
            RandomNumberGenerator.Fill(bytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private static string HashToken(string rawToken)
        {
            var bytes = Encoding.UTF8.GetBytes(rawToken);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
