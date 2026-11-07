using DineBook.Application.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Pages.Payment
{
    // Cashfree calls this URL after a payment. It is called by a server, not a browser, so no antiforgery token is sent.
    // Trust comes from the HMAC signature, checked inside PaymentService.HandleWebhookAsync.
    // Errors are turned into status codes by DomainExceptionFilter (see Infrastructure).
    [IgnoreAntiforgeryToken]
    public class WebhookModel : PageModel
    {
        // Real webhook bodies are small. Anything larger is not from Cashfree.
        private const int MaxBodyBytes = 64 * 1024;

        private readonly IPaymentService _payments;

        public WebhookModel(IPaymentService payments)
        {
            _payments = payments;
        }

        // Only POST is meaningful here
        public IActionResult OnGet() => NotFound();

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            var timestamp = Request.Headers["x-webhook-timestamp"].ToString();
            var signature = Request.Headers["x-webhook-signature"].ToString();

            if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature))
                return Unauthorized();

            if (Request.ContentLength is > MaxBodyBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge);

            // The signature is over the exact bytes Cashfree sent, so read them raw
            var buffer = new char[MaxBodyBytes + 1];
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var read = await reader.ReadBlockAsync(buffer.AsMemory(0, buffer.Length), ct);
            if (read > MaxBodyBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge);

            var rawBody = new string(buffer, 0, read);

            var accepted = await _payments.HandleWebhookAsync(rawBody, timestamp, signature, ct);

            // 401 tells Cashfree the signature was wrong. Valid events get 200, even when already processed.
            return accepted ? new OkResult() : Unauthorized();
        }
    }
}
