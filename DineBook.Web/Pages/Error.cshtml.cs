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

        public string ErrorDescription { get; set; }

        public void OnGet(int statusCode)
        {
            ErrorStatusCode = statusCode;

            switch (statusCode)
            {
                case StatusCodes.Status400BadRequest:
                    ErrorMessage = "Bad Request";
                    ErrorDescription = "The server could not understand the request due to invalid syntax.";
                    break;

                case StatusCodes.Status401Unauthorized:
                    ErrorMessage = "Unauthorized";
                    ErrorDescription = "The client must authenticate itself to get the requested response.";
                    break;

                case StatusCodes.Status403Forbidden:
                    ErrorMessage = "Forbidden";
                    ErrorDescription = "The client does not have access rights to the content.";
                    break;

                case StatusCodes.Status404NotFound:
                    ErrorMessage = "Not Found";
                    ErrorDescription = "The requested resource could not be found.";
                    break;

                case StatusCodes.Status500InternalServerError:
                    ErrorMessage = "Internal Server Error";
                    ErrorDescription = "The server has encountered a situation it doesn't know how to handle.";
                    break;

                case StatusCodes.Status503ServiceUnavailable:
                    ErrorMessage = "Website is down for maintenance";
                    ErrorDescription = "The server is not ready to handle the request. Common causes are a server that is down for maintenance or that is overloaded.";
                    break;
            }
        }
    }
}