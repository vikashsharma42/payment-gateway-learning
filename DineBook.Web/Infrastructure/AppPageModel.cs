using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace DineBook.Web.Infrastructure
{
    public abstract class AppPageModel : PageModel
    {
        private const string ToastKey = "Toast";

        // The signed-in account's id (set from the cookie at sign-in). 0 when nobody is signed in.
        protected long UserId =>
            long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        protected void ToastSuccess(string message) => SetToast("success", message);

        protected void ToastError(string message) => SetToast("danger", message);

        private void SetToast(string type, string message) =>
            TempData[ToastKey] = JsonSerializer.Serialize(new ToastMessage(type, message));
    }

    // Read by _Layout.cshtml and shown by app.js
    public sealed record ToastMessage(string Type, string Message);
}
