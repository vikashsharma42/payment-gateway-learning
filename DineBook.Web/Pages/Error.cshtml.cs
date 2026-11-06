using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace DineBook.Web.Pages
{
    [OutputCache]
    public class ErrorModel : PageModel
    {
        public int ErrorStatusCode { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet(int statusCode)
        {
            ErrorStatusCode = statusCode;

            switch (statusCode)
            {
                case StatusCodes.Status400BadRequest:
                    ErrorMessage = "Bad Request";
                    break;

                case StatusCodes.Status401Unauthorized:
                    ErrorMessage = "Unauthorized";
                    break;

                case StatusCodes.Status403Forbidden:
                    ErrorMessage = "Forbidden";
                    break;

                case StatusCodes.Status404NotFound:
                    ErrorMessage = "Not Found";
                    break;

                case StatusCodes.Status500InternalServerError:
                    ErrorMessage = "Internal Server Error";
                    break;

                case StatusCodes.Status503ServiceUnavailable:
                    ErrorMessage = "Website is down for maintenance";
                    break;
            }
        }
    }
}