using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<Features.Users.AppUser> _signInManager;
    private readonly UserManager<Features.Users.AppUser> _userManager;
    private readonly IConfiguration _config;

    public LoginModel(SignInManager<Features.Users.AppUser> signInManager, UserManager<Features.Users.AppUser> userManager, IConfiguration config)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _config = config;
    }

    public bool TestLoginEnabled => !string.IsNullOrWhiteSpace(_config["TestUser__Email"]) && !string.IsNullOrWhiteSpace(_config["TestUser__Password"]);

    public string TestUserEmail => _config["TestUser__Email"] ?? string.Empty;

    public async Task<IActionResult> OnPostMicrosoftAsync(string? returnUrl = null)
    {
        var props = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnPostTestAsync(string? returnUrl = null)
    {
        var email = _config["TestUser__Email"];
        var password = _config["TestUser__Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return RedirectToPage();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Test user not found.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return Redirect(returnUrl ?? "/");
        }

        ModelState.AddModelError(string.Empty, "Invalid test login.");
        return Page();
    }
}
