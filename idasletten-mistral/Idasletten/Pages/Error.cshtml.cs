using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
    public int ErrorCode { get; set; } = 500;
    public string ErrorMessage { get; set; } = "An unexpected error occurred";
    
    public void OnGet()
    {
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        
        if (exceptionHandlerPathFeature?.Error != null)
        {
            ErrorMessage = exceptionHandlerPathFeature.Error.Message;
        }
        else if (exceptionHandlerFeature?.Error != null)
        {
            ErrorMessage = exceptionHandlerFeature.Error.Message;
        }
        else
        {
            ErrorCode = Response.StatusCode;
            ErrorMessage = ErrorCode switch
            {
                404 => "The page you requested could not be found.",
                403 => "You do not have permission to access this page.",
                500 => "An unexpected error occurred. Please try again later.",
                _ => $"Error {ErrorCode}"
            };
        }
    }
}
