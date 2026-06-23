using System.Security.Claims;
using Idasletten.Shared.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class LoginModel : PageModel
{
    private readonly TestUserConfig? _testUserConfig;

    public LoginModel(TestUserConfig? testUserConfig = null)
    {
        _testUserConfig = testUserConfig;
    }

    public bool TestUserEnabled => _testUserConfig != null;
    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl ?? "/";
    }

    public async Task<IActionResult> OnPostTestLoginAsync(string? returnUrl)
    {
        if (_testUserConfig == null)
            return Forbid();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, _testUserConfig.Email),
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim("preferred_username", "TEST"),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return LocalRedirect(returnUrl ?? "/");
    }
}
