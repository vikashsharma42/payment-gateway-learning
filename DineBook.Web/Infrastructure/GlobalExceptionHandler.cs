using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DineBook.Web.Infrastructure
{
    /// <summary>
    /// Logs every unhandled exception once. It does not write the response: returning false lets
    /// UseExceptionHandler("/Error/500") show the friendly error page.
    /// Logs contain the exception and the path only, never form values (email, phone, card data).
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled error on {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            return ValueTask.FromResult(false);
        }
    }
}
