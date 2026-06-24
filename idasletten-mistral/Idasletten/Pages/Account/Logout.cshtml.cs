using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Account;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync()
    {
        // Sign out of both cookie and OpenID Connect schemes
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        
        return LocalRedirect("/");
    }
}
