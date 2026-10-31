using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProductShop.Email
{
    /// <summary>
    /// Sends transactional email through the Brevo (formerly Sendinblue) SMTP API.
    /// </summary>
    public sealed class BrevoEmailSender : IEmailSender
    {
        private const string EndpointPath = "https://api.brevo.com/v3/smtp/email";

        private readonly HttpClient _http;
        private readonly EmailOptions _options;

        public BrevoEmailSender(HttpClient http, IOptions<EmailOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            var payload = new
            {
                sender = new { email = _options.SenderEmail, name = _options.SenderName },
                to = new[] { new { email = message.To } },
                subject = message.Subject,
                htmlContent = message.HtmlBody
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, EndpointPath)
            {
                Content = JsonContent.Create(payload)
            };
            req.Headers.Add("api-key", _options.ApiKey);

            using var res = await _http.SendAsync(req, ct);

            // Do not include the response body: it can echo the recipient address
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Brevo rejected the email with status {(int)res.StatusCode}.");
        }
    }
}