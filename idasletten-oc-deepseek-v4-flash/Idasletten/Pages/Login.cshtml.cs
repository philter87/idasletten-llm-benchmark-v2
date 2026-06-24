using Idasletten.Shared.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public LoginModel(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public bool TestUserEnabled { get; set; }

    public void OnGet()
    {
        TestUserEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestUser__Email"))
            && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TestUser__Password"));
    }

    public IActionResult OnGetMicrosoft()
    {
        var redirectUrl = Url.Page("/Login", "MicrosoftCallback");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Microsoft", redirectUrl);
        return new ChallengeResult("Microsoft", properties);
    }

    public async Task<IActionResult> OnGetMicrosoftCallbackAsync(string? remoteError = null)
    {
        if (remoteError != null)
            return RedirectToPage("/Login");

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return RedirectToPage("/Login");

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
        if (result.Succeeded)
            return RedirectToPage("/Index");

        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        var user = new User
        {
            UserName = email ?? Guid.NewGuid().ToString(),
            Initials = email?.Substring(0, Math.Min(3, email.Length)).ToUpperInvariant() ?? "USR",
            Name = name ?? email ?? "User",
            Email = email
        };

        var createResult = await _userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, false);
        }

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostTestLoginAsync()
    {
        var email = Environment.GetEnvironmentVariable("TestUser__Email");
        var password = Environment.GetEnvironmentVariable("TestUser__Password");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return RedirectToPage("/Login");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return RedirectToPage("/Login");

        var result = await _signInManager.PasswordSignInAsync(user.UserName, password, false, false);
        if (result.Succeeded)
            return RedirectToPage("/Index");

        return RedirectToPage("/Login");
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}
