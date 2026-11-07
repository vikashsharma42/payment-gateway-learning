using DineBook.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace DineBook.Web.Infrastructure
{
    /// <summary>
    /// Global middleware for rule violations (DomainException).
    /// Form POSTs redirect back to the same page with a toast (Post/Redirect/Get).
    /// GET requests go to the home page, so a bad URL cannot loop.
    /// Payment webhooks get status codes: 200 for a valid event that needs no change, 400 for a malformed body.
    /// Every other exception continues to UseExceptionHandler, which logs it and shows the error page.
    /// </summary>
    public sealed class DomainExceptionMiddleware
    {
        private const string ToastKey = "Toast";
        private const string WebhookPath = "/Payment/Webhook";

        private readonly RequestDelegate _next;

        public DomainExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITempDataDictionaryFactory tempDataFactory,
            ILogger<DomainExceptionMiddleware> logger)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (IsWebhook(context) && !context.Response.HasStarted)
            {
                await WriteWebhookStatus(context, ex, logger);
            }
            catch (DomainException domain) when (!context.Response.HasStarted)
            {
                logger.LogInformation("Rule violation on {Path}: {Message}", context.Request.Path, domain.Message);

                var temp = tempDataFactory.GetTempData(context);
                temp[ToastKey] = JsonSerializer.Serialize(new ToastMessage("danger", domain.Message));

                var target = context.Request.Method == HttpMethods.Post
                    ? context.Request.PathBase + context.Request.Path + context.Request.QueryString
                    : "/Index";

                context.Response.Redirect(target);
            }
        }

        private static bool IsWebhook(HttpContext context) =>
            context.Request.Path.StartsWithSegments(WebhookPath, StringComparison.OrdinalIgnoreCase);

        private static Task WriteWebhookStatus(HttpContext context, Exception ex, ILogger logger)
        {
            if (ex is DomainException domain)
            {
                // Valid signature, nothing to change (for example, already processed): acknowledge it
                logger.LogInformation("Webhook acknowledged without change: {Message}", domain.Message);
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            }

            if (ex is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                // Malformed body. Retrying will not help. Log without the body.
                logger.LogWarning(ex, "Malformed Cashfree webhook received");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Task.CompletedTask;
            }

            ExceptionDispatchInfo.Capture(ex).Throw();
            return Task.CompletedTask;
        }
    }
}