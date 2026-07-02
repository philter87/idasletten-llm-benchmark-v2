using System.Security.Claims;
using Idasletten.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class LoginModel(IConfiguration configuration, IMediator mediator) : PageModel
{
    public bool AzureAdEnabled => !string.IsNullOrEmpty(configuration["AzureAd:ClientId"]);

    /// <summary>Test login is only available when both env vars are set (see AGENTS.md).</summary>
    public bool TestUserEnabled =>
        !string.IsNullOrEmpty(configuration["TestUser:Email"]) &&
        !string.IsNullOrEmpty(configuration["TestUser:Password"]);

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostTestLogin(string email, string password)
    {
        if (!TestUserEnabled
            || !string.Equals(email, configuration["TestUser:Email"], StringComparison.OrdinalIgnoreCase)
            || password != configuration["TestUser:Password"])
        {
            ErrorMessage = "Forkert e-mail eller adgangskode.";
            return Page();
        }

        var initials = new string(email.TakeWhile(c => c != '@').Take(3).ToArray()).ToUpperInvariant();
        var user = await mediator.Send(new CreateUserCommand(initials, "Test User", email));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl);
    }
}
