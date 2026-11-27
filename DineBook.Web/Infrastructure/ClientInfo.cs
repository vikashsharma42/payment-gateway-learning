using Microsoft.AspNetCore.Http;

namespace DineBook.Web.Infrastructure
{
    // Address and browser of the caller, for sign-in limits and the security log
    public static class ClientInfo
    {
        public static string Address(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        public static string Agent(HttpContext context) =>
            context.Request.Headers["User-Agent"].ToString();
    }
}
