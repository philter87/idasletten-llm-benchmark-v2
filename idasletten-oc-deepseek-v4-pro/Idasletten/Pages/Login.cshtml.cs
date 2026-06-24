using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public bool HasAzureAd { get; set; }
    public bool HasTestUser { get; set; }
    public string? ErrorMessage { get; set; }

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        HasAzureAd = !string.IsNullOrEmpty(_configuration["AzureAd:ClientId"]);
        HasTestUser = !string.IsNullOrEmpty(_configuration["TestUser:Email"])
            && !string.IsNullOrEmpty(_configuration["TestUser:Password"]);
    }

    public async Task<IActionResult> OnPostAzureLoginAsync()
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "OpenIdConnect");
    }

    public async Task<IActionResult> OnPostTestLoginAsync(string email, string password)
    {
        var configuredEmail = _configuration["TestUser:Email"];
        var configuredPassword = _configuration["TestUser:Password"];

        if (email != configuredEmail || password != configuredPassword)
        {
            ErrorMessage = "Invalid test credentials.";
            HasAzureAd = !string.IsNullOrEmpty(_configuration["AzureAd:ClientId"]);
            HasTestUser = true;
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToPage("/Index");
    }
}
