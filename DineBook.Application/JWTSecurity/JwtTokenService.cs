using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DineBook.Application.JWTSecurity
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _settings;
        private readonly IMemoryCache _cache;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(IOptions<JwtOptions> settings, IMemoryCache cache, ILogger<JwtTokenService> logger)
        {
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string> GenerateJwtTokenAsync(IList<Claim> claims)
        {
            ArgumentNullException.ThrowIfNull(claims);
            var token = GenerateJwtToken(claims, _settings.TokenExpiry);
            return Task.FromResult(token);
        }

        public async Task<bool> ValidateJwtTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_settings.Key);

                var validationParameter = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _settings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principle = await tokenHandler.ValidateTokenAsync(token, validationParameter);
                return principle.IsValid;
            }
            catch (SecurityTokenException ex)
            {
                // Expected for expired / tampered tokens — debug level is appropriate.
                _logger.LogDebug(ex, "JWT validation failed (token rejected).");
                return false;
            }
            catch (Exception ex)
            {
                // Unexpected — misconfiguration, infrastructure fault, etc.
                _logger.LogWarning(ex, "Unexpected error during JWT validation.");
                return false;
            }
        }

        public Task InvalidateTokenAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
                return Task.CompletedTask;

            var cachekey = $"token:blacklist:{jti}";
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _settings.TokenExpiry
            };
            _cache.Set(cachekey, true, cacheOptions);
            return Task.CompletedTask;
        }

        public bool IsTokenBlacklisted(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
                return false;

            var cachekey = $"token:blacklist:{jti}";
            return _cache.TryGetValue(cachekey, out _);
        }

        private string GenerateJwtToken(IEnumerable<Claim> claims, TimeSpan expiry)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
               issuer: _settings.Issuer,
               audience: _settings.Audience,
               claims: claims,
               notBefore: now,
               expires: now.Add(expiry),
               signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
