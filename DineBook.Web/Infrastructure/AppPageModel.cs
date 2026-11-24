using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Security.Claims;
using System.Text.Json;

namespace DineBook.Web.Infrastructure
{
    public abstract class AppPageModel : PageModel
    {
        private const string ToastKey = "Toast";

        // Id of the signed-in account. Only read on pages that require sign-in.
        protected long UserId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new InvalidOperationException("The signed-in account id is missing.");

        protected void ToastSuccess(string message) => SetToast("success", message);

        protected void ToastError(string message) => SetToast("danger", message);

        private void SetToast(string type, string message) =>
            TempData[ToastKey] = JsonSerializer.Serialize(new ToastMessage(type, message));
    }

    // Read by _Layout.cshtml and shown by app.js
    public sealed record ToastMessage(string Type, string Message);
}
