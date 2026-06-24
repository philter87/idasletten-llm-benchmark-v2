using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Idasletten.Pages;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public LoginModel(IConfiguration configuration) => _configuration = configuration;

    public bool TestLoginEnabled =>
        !string.IsNullOrEmpty(_configuration["TestUser:Email"]) &&
        !string.IsNullOrEmpty(_configuration["TestUser:Password"]);

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");
        return Page();
    }

    public IActionResult OnGetChallenge(string? returnUrl = null)
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = returnUrl ?? "/"
        }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnPostTestLoginAsync(string email, string password, string? returnUrl = null)
    {
        var testEmail = _configuration["TestUser:Email"];
        var testPassword = _configuration["TestUser:Password"];

        if (string.IsNullOrEmpty(testEmail) || string.IsNullOrEmpty(testPassword))
        {
            ModelState.AddModelError("", "Test login is not configured");
            return Page();
        }

        if (!string.Equals(email, testEmail, StringComparison.OrdinalIgnoreCase) || password != testPassword)
        {
            ModelState.AddModelError("", "Invalid credentials");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, testEmail),
            new("preferred_username", "TEST")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Redirect(returnUrl ?? "/");
    }
}
