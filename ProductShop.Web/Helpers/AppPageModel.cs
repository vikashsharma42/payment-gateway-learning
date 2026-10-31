using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ProductShop.Web.Helpers
{
    public abstract class AppPageModel : PageModel
    {
        private const string ToastKey = "Toast";

        protected void ToastSuccess(string message) => SetToast("success", message);

        protected void ToastError(string message) => SetToast("danger", message);

        private void SetToast(string type, string message) =>
            TempData[ToastKey] = JsonSerializer.Serialize(new ToastMessage(type, message));
    }

    // Read by _Layout.cshtml and shown by app.js
    public sealed record ToastMessage(string Type, string Message);
}