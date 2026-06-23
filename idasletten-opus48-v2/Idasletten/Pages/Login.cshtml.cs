using System.Security.Claims;
using Idasletten.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages;

public class LoginModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public LoginModel(IConfiguration config, AppDbContext db)
    {
        _config = config;
        _db = db;
    }

    public bool AzureEnabled =>
        !string.IsNullOrWhiteSpace(_config["AzureAd:ClientId"]) &&
        !string.IsNullOrWhiteSpace(_config["AzureAd:Instance"]);

    /// <summary>The test-user login is only offered when both env vars are configured.</summary>
    public bool TestLoginEnabled =>
        !string.IsNullOrWhiteSpace(_config["TestUser:Email"]) &&
        !string.IsNullOrWhiteSpace(_config["TestUser:Password"]);

    [BindProperty] public string? Email { get; set; }
    [BindProperty] public string? Password { get; set; }
    public string? Error { get; private set; }

    public void OnGet() { }

    public IActionResult OnPostMicrosoft(string? returnUrl)
    {
        if (!AzureEnabled) return RedirectToPage(new { returnUrl });
        return Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl ?? Url.Content("~/") },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnPostTest(string? returnUrl)
    {
        if (!TestLoginEnabled ||
            !string.Equals(Email, _config["TestUser:Email"], StringComparison.OrdinalIgnoreCase) ||
            Password != _config["TestUser:Password"])
        {
            Error = "Invalid test credentials.";
            return Page();
        }

        var normalizedEmail = Email!.ToUpperInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user?.Id.ToString() ?? Guid.Empty.ToString()),
            new(ClaimTypes.Name, user?.UserName ?? Email!),
            new(ClaimTypes.Email, Email!)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return LocalRedirect(returnUrl ?? Url.Content("~/"));
    }

    public async Task<IActionResult> OnPostLogout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (AzureEnabled)
            return SignOut(OpenIdConnectDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}
