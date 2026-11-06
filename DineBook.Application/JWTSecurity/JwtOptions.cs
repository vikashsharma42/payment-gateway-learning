using System;
using System.ComponentModel.DataAnnotations;

namespace DineBook.Application.JWTSecurity
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        /// <summary>
        /// HMAC-SHA256 signing key. Must be at least 32 characters (256 bits). Keep it in user-secrets.
        /// </summary>
        [Required(ErrorMessage = "Jwt:Key is required. Store it in user-secrets.")]
        [MinLength(32, ErrorMessage = "JWT signing key must be at least 32 characters (256 bits).")]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Lifetime of an access token (e.g. "00:15:00" for 15 minutes).
        /// </summary>
        public TimeSpan TokenExpiry { get; set; } = TimeSpan.FromMinutes(15);

        [Range(1, 90, ErrorMessage = "Refresh token expiry days must be between 1 and 90 days.")]
        public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}
