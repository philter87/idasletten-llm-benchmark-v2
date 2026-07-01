using Idasletten.Data;
using Idasletten.Shared.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Idasletten.Pages;

public class LoginModel(IdaslettenDbContext db, IOptions<TestUserOptions> testUserOptions) : PageModel
{
    public bool TestLoginEnabled => testUserOptions.Value.IsEnabled;

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public IActionResult OnPostMicrosoft(string? returnUrl = null)
    {
        var redirectUri = string.IsNullOrEmpty(returnUrl) ? Url.Page("/Index") : returnUrl;
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnPostTestLoginAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!TestLoginEnabled)
        {
            return NotFound();
        }

        var options = testUserOptions.Value;
        if (Email != options.Email || Password != options.Password)
        {
            ErrorMessage = "Invalid test-user email or password.";
            return Page();
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == options.Email);
        if (user is null)
        {
            ErrorMessage = "Test user is not seeded. Check TestUser__Email / TestUser__Password.";
            return Page();
        }

        await SignInHelper.SignInAsync(HttpContext, user);

        return string.IsNullOrEmpty(returnUrl) ? RedirectToPage("/Index") : LocalRedirect(returnUrl);
    }
}
