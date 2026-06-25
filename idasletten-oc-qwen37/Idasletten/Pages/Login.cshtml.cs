using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool ShowTestLogin { get; set; }

    public void OnGet()
    {
        var testEmail = _configuration["TestUser:Email"];
        var testPassword = _configuration["TestUser:Password"];
        ShowTestLogin = !string.IsNullOrEmpty(testEmail) && !string.IsNullOrEmpty(testPassword);
    }

    public IActionResult OnPostAzureAd()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult OnPostTestUser(string email, string password)
    {
        var testEmail = _configuration["TestUser:Email"];
        var testPassword = _configuration["TestUser:Password"];

        if (email == testEmail && password == testPassword)
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.Name, "Test User"),
                new(System.Security.Claims.ClaimTypes.Email, email),
                new(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal).Wait();
            return RedirectToPage("/Index");
        }

        ModelState.AddModelError(string.Empty, "Invalid credentials");
        return Page();
    }
}
