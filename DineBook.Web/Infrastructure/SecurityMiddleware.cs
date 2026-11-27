using System.Threading;
using DineBook.Application.Services.Interface;
using DineBook.Domain.Enums;
using DineBook.Domain.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DineBook.Web.Infrastructure
{
    // Runs before routing on every request. It refuses addresses at critical risk, scanner tools and probes,
    // and input that carries attack patterns. It records what it sees for the security log.
    // If the monitor itself fails, the failure is logged and the request continues.
    public sealed class SecurityMiddleware
    {
        private const int MaxInspectedBodyBytes = 64 * 1024;
        private static readonly TimeSpan RiskCacheTime = TimeSpan.FromSeconds(30);

        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;

        public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISecurityService security, IMemoryCache cache)
        {
            if (!await ScreenAsync(context, security, cache))
                return;

            await _next(context);

            // Razor Pages answers a form post with a missing or expired antiforgery token with 400
            if (HttpMethods.IsPost(context.Request.Method) && context.Response.StatusCode == StatusCodes.Status400BadRequest)
                await TryRecordAsync(security, context, SecurityEventKind.CsrfRejected, "Form post refused: missing or expired form token");
        }

        // True when the request may continue. False when it was refused and the response is written.
        private async Task<bool> ScreenAsync(HttpContext context, ISecurityService security, IMemoryCache cache)
        {
            var address = ClientInfo.Address(context);
            var agent = ClientInfo.Agent(context);
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;
            var ct = context.RequestAborted;

            try
            {
                // 1. Addresses at critical risk are refused for a while. The score is cached briefly to save database reads.
                var score = await cache.GetOrCreateAsync("risk:" + address, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = RiskCacheTime;
                    return security.RiskScoreAsync(address, ct);
                });

                if (RiskScoring.IsRestricted(score))
                    return await RefuseAsync(context, security, SecurityEventKind.RiskRestricted, address, agent, method, path,
                        "Refused: address at critical risk", StatusCodes.Status403Forbidden,
                        "Your access has been temporarily restricted. Please try again later or contact the restaurant.");

                // 2. Scanner tools and probes for software this site does not run
                if (ThreatDetector.IsScanner(agent))
                    return await RefuseAsync(context, security, SecurityEventKind.ScannerDetected, address, agent, method, path,
                        "Security scanner user agent", StatusCodes.Status403Forbidden, "Request refused.");

                if (ThreatDetector.IsHoneypot(path))
                    return await RefuseAsync(context, security, SecurityEventKind.HoneypotProbe, address, agent, method, path,
                        "Probe of a path this site does not use", StatusCodes.Status404NotFound, "Not found.");

                // 3. Attack patterns in the query string and form fields
                var inputs = await ReadInputsAsync(context);

                // Honeypot: a filled-in hidden field means a form-filling bot
                if (HttpMethods.IsPost(method)
                    && inputs.Exists(i => i.Name == ThreatDetector.HoneypotField && !string.IsNullOrWhiteSpace(i.Value)))
                    return await RefuseAsync(context, security, SecurityEventKind.HoneypotProbe, address, agent, method, path,
                        "Hidden form field was filled in", StatusCodes.Status400BadRequest, "This request was blocked for security reasons.");

                var findings = ThreatDetector.Inspect(inputs);
                if (findings.Count > 0)
                {
                    foreach (var finding in findings)
                        await security.RecordAsync(finding.Kind, address, agent, method, path, string.Empty, finding.Detail, true, ct);

                    await WriteAsync(context, StatusCodes.Status400BadRequest, "This request was blocked for security reasons.");
                    return false;
                }

                // 4. Scripted form posts are logged so the monitor can see automation against the site
                if (HttpMethods.IsPost(method) && ThreatDetector.IsAutomated(agent))
                    await security.RecordAsync(SecurityEventKind.AutomatedClient, address, agent, method, path, string.Empty,
                        "Form post from an automated client", false, ct);

                // 5. Request rate: a very fast client is logged as automation once per minute. It is not refused here.
                await CountRequestAsync(address, agent, method, path, security, cache, ct);

                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "The security monitor could not check a request");
                return true;
            }
        }

        // Requests per address in each minute. Crossing the limit is logged once for that minute.
        private const int RequestsPerMinuteLimit = 180;

        private static async Task CountRequestAsync(string address, string agent, string method, string path,
            ISecurityService security, IMemoryCache cache, CancellationToken ct)
        {
            var minute = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
            var counter = cache.GetOrCreate($"rate:{address}:{minute}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                return new int[1];
            });

            var count = Interlocked.Increment(ref counter[0]);
            if (count == RequestsPerMinuteLimit + 1)
                await security.RecordAsync(SecurityEventKind.AutomatedClient, address, agent, method, path, string.Empty,
                    $"High request rate: more than {RequestsPerMinuteLimit} requests in one minute", false, ct);
        }

        private static async Task<bool> RefuseAsync(HttpContext context, ISecurityService security, SecurityEventKind kind,
            string address, string agent, string method, string path, string detail, int status, string message)
        {
            await security.RecordAsync(kind, address, agent, method, path, string.Empty, detail, true, context.RequestAborted);
            await WriteAsync(context, status, message);
            return false;
        }

        private static async Task WriteAsync(HttpContext context, int status, string message)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(message, context.RequestAborted);
        }

        private async Task TryRecordAsync(ISecurityService security, HttpContext context, SecurityEventKind kind, string detail)
        {
            try
            {
                await security.RecordAsync(kind, ClientInfo.Address(context), ClientInfo.Agent(context), context.Request.Method,
                    context.Request.Path.Value ?? string.Empty, string.Empty, detail, false, context.RequestAborted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "A security event could not be saved");
            }
        }

        // Query string values, plus form values for small form posts. Large or streamed bodies are not inspected.
        private static async Task<List<(string Name, string Value)>> ReadInputsAsync(HttpContext context)
        {
            var inputs = new List<(string Name, string Value)>();

            foreach (var pair in context.Request.Query)
                foreach (var value in pair.Value)
                    inputs.Add((pair.Key, value ?? string.Empty));

            var request = context.Request;
            if (HttpMethods.IsPost(request.Method) && request.HasFormContentType
                && request.ContentLength is { } length && length <= MaxInspectedBodyBytes)
            {
                // The form is kept by ASP.NET Core, so the page can still bind it
                var form = await request.ReadFormAsync(context.RequestAborted);
                foreach (var pair in form)
                    foreach (var value in pair.Value)
                        inputs.Add((pair.Key, value ?? string.Empty));
            }

            return inputs;
        }
    }
}
