using DineBook.Application.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Payment
{
    // Cashfree calls this server to server when a payment succeeds. Anonymous, and no form token.
    [IgnoreAntiforgeryToken]
    public class WebhookModel : PageModel
    {
        private const int MaxBodyBytes = 64 * 1024;

        private readonly IPaymentService _payments;

        public WebhookModel(IPaymentService payments)
        {
            _payments = payments;
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (Request.ContentLength is > MaxBodyBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge);

            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync(ct);
            }

            if (Encoding.UTF8.GetByteCount(body) > MaxBodyBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge);

            var timestamp = Request.Headers["x-webhook-timestamp"].ToString();
            var signature = Request.Headers["x-webhook-signature"].ToString();

            try
            {
                // Returns false when the signature does not match. Payments are only marked paid after the amount is checked.
                var accepted = await _payments.HandleWebhookAsync(body, timestamp, signature, ct);
                return accepted ? StatusCode(StatusCodes.Status200OK) : StatusCode(StatusCodes.Status401Unauthorized);
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                // Signed but malformed body
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }
    }
}
