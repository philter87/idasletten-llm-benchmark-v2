using System.Security.Claims;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Idasletten.Pages;

public class LoginModel(
    ISender sender,
    IOptions<TestUserOptions> testUserOptions,
    IConfiguration configuration) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? ErrorMessage { get; private set; }

    public bool AzureAdEnabled =>
        !string.IsNullOrWhiteSpace(configuration["AzureAd:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["AzureAd:TenantId"]);

    /// <summary>The test login is only there when both TestUser__Email and TestUser__Password are set.</summary>
    public bool TestLoginEnabled => testUserOptions.Value.IsEnabled;

    public void OnGet()
    {
    }

    public IActionResult OnPostMicrosoft()
    {
        if (!AzureAdEnabled)
        {
            return RedirectToPage();
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = SafeReturnUrl() },
            AuthConstants.AzureAdScheme);
    }

    public async Task<IActionResult> OnPostTestUserAsync()
    {
        var testUser = testUserOptions.Value;
        if (!testUser.Matches(Email, Password))
        {
            ErrorMessage = "Forkert e-mail eller adgangskode til testbrugeren.";
            return Page();
        }

        var user = await sender.Send(new GetOrCreateUser(testUser.Initials, testUser.Name, testUser.Email));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(AuthConstants.AppUserIdClaim, user.Id.ToString()),
            new(AuthConstants.InitialsClaim, user.Initials),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return Redirect(SafeReturnUrl());
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }

    private string SafeReturnUrl() =>
        !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";
}
