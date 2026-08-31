using Idasletten.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Account;

public class SignOutModel : PageModel
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

    public SignOutModel(Microsoft.Extensions.Configuration.IConfiguration config) => _config = config;

    public async Task OnPostAsync()
    {
        await HttpContext.SignOutAsync(AuthConstants.AppScheme);
        if (!string.IsNullOrWhiteSpace(_config["AzureAd:ClientId"]))
            await HttpContext.SignOutAsync(AuthConstants.AzureAdScheme);
        Response.Redirect("/");
    }
}
