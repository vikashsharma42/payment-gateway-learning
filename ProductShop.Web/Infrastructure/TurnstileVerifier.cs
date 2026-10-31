using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Web.Infrastructure
{
    /// <summary>
    /// Verifies a Cloudflare Turnstile token on the server. Fails closed: with no secret
    /// configured, or any error, the check is rejected.
    /// </summary>
    public sealed class TurnstileVerifier
    {
        private static readonly Uri VerifyUri = new Uri("https://challenges.cloudflare.com/turnstile/v0/siteverify");

        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TurnstileVerifier> _logger;

        public TurnstileVerifier(HttpClient http, IConfiguration configuration, ILogger<TurnstileVerifier> logger)
        {
            _http = http;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> VerifyAsync(string token, string remoteIp, CancellationToken ct)
        {
            var secret = _configuration["Turnstile:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("Turnstile:SecretKey is not configured. Every checkout will be rejected.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Turnstile token missing from checkout form. Widget may not have rendered (check Turnstile:SiteKey).");
                return false;
            }

            var form = new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = token
            };

            if (!string.IsNullOrEmpty(remoteIp))
                form["remoteip"] = remoteIp;

            try
            {
                using var content = new FormUrlEncodedContent(form);
                using var response = await _http.PostAsync(VerifyUri, content, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Turnstile siteverify returned HTTP {Status}.", (int)response.StatusCode);
                    return false;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                var root = doc.RootElement;
                var ok = root.TryGetProperty("success", out var success) && success.GetBoolean();

                if (!ok)
                {
                    // Common codes: invalid-input-secret (secret wrong), invalid-input-response
                    // (token wrong or used/expired), timeout-or-duplicate, bad-request
                    var codes = root.TryGetProperty("error-codes", out var errorCodes) ? errorCodes.GetRawText() : "none";
                    _logger.LogWarning("Turnstile rejected the token. error-codes: {Codes}", codes);
                }

                return ok;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }
    }
}