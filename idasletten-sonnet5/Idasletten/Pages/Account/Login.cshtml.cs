using System.Security.Claims;
using Idasletten.Shared.Auth;
using Idasletten.Shared.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Account;

public class LoginModel(IdaslettenDbContext db, TestUserOptions testUserOptions, AzureAdAvailability azureAdAvailability) : PageModel
{
    public bool TestLoginEnabled { get; private set; }
    public bool MicrosoftLoginEnabled => azureAdAvailability.Configured;

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? Error { get; private set; }

    public void OnGet()
    {
        TestLoginEnabled = testUserOptions.Enabled;
    }

    public IActionResult OnPostMicrosoft(string? returnUrl)
    {
        if (!azureAdAvailability.Configured)
        {
            return BadRequest("Azure AD is not configured.");
        }
        var properties = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
        return Challenge(properties, "AzureAD");
    }

    public async Task<IActionResult> OnPostTestLoginAsync(string? returnUrl)
    {
        TestLoginEnabled = testUserOptions.Enabled;

        if (!testUserOptions.Enabled || Email != testUserOptions.Email || Password != testUserOptions.Password)
        {
            Error = "Invalid test-user credentials.";
            return Page();
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == Email);
        if (user is null)
        {
            Error = "Test user is not seeded in the database.";
            return Page();
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email!));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Redirect(returnUrl ?? "/");
    }
}
