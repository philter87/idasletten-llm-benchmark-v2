using System.Security.Claims;
using Idasletten.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages;

public class LoginModel(IConfiguration configuration, IdaslettenDbContext db) : PageModel
{
    public bool TestLoginEnabled => !string.IsNullOrWhiteSpace(configuration["TestUser:Email"]) &&
                                    !string.IsNullOrWhiteSpace(configuration["TestUser:Password"]);
    public bool AzureEnabled => !string.IsNullOrWhiteSpace(configuration["AzureAd:ClientId"]);

    public void OnGet() { }

    public ChallengeResult OnPostMicrosoft(string? returnUrl) =>
        Challenge(new AuthenticationProperties { RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/" }, "AzureAD");

    public async Task<IActionResult> OnPostTestAsync(string password, string? returnUrl)
    {
        var email = configuration["TestUser:Email"];
        if (!TestLoginEnabled || password != configuration["TestUser:Password"])
        {
            ModelState.AddModelError(string.Empty, "The test password is not valid.");
            return Page();
        }
        var user = await db.Users.SingleAsync(x => x.Email == email);
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Name)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}
