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
    private readonly IWebHostEnvironment _environment;

    public LoginModel(SignInManager<Features.Users.AppUser> signInManager, UserManager<Features.Users.AppUser> userManager, IConfiguration config, IWebHostEnvironment environment)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _config = config;
        _environment = environment;
    }

    public bool TestLoginEnabled => !string.IsNullOrWhiteSpace(TestUserEmail) && !string.IsNullOrWhiteSpace(TestUserPassword);

    public string TestUserEmail => GetSetting("TestUser__Email", "test@idasletten.local");

    public string TestUserPassword => GetSetting("TestUser__Password", "Test1234!");

    public async Task<IActionResult> OnPostMicrosoftAsync(string? returnUrl = null)
    {
        var props = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnPostTestAsync(string? returnUrl = null)
    {
        var email = TestUserEmail;
        var password = TestUserPassword;
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

    private string GetSetting(string key, string fallback)
    {
        var value = _config[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return _environment.IsEnvironment("Testing") ? fallback : string.Empty;
    }
}
