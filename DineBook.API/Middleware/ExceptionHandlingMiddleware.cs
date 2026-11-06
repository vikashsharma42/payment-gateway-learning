using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DineBook.Domain.Exceptions;
using System;
using System.Threading.Tasks;

namespace DineBook.API.Middleware
{
    /// <summary>
    /// Converts exceptions into RFC 7807 ProblemDetails responses.
    /// DomainException -> 400 (business rule), anything else → 500 (details only logged).
    /// </summary>
    public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (DomainException ex)
            {
                await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Business rule violated", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.");
            }
        }

        private static Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
        {
            context.Response.StatusCode = status;

            return context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            });
        }
    }
}