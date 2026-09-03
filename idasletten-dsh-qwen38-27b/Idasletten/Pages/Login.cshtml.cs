using Idasletten.Auth;
using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IOptions<TestUserOptions> _testUser;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

    public LoginModel(AppDbContext db, IOptions<TestUserOptions> testUser, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _db = db;
        _testUser = testUser;
        _config = config;
    }

    public bool TestUserEnabled => _testUser.Value.Enabled;
    public bool AzureAdEnabled => !string.IsNullOrWhiteSpace(_config["AzureAd:ClientId"]);

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public async Task OnGetAsync()
    {
    }

    public async Task OnPostTestLoginAsync()
    {
        if (!TestUserEnabled)
        {
            TempData["Error"] = "Test login is not enabled.";
            return;
        }

        var email = (Email ?? "").Trim();
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == email.ToLower() && u.PasswordHash != null);
        if (user is null || !PasswordHasher.Verify(Password ?? string.Empty, user.PasswordHash))
        {
            TempData["Error"] = "Invalid email or password.";
            return;
        }

        await SignInAsAppUserAsync(user);
        Response.Redirect(SafeReturnUrl());
    }

    /// <summary>Triggers the Azure AD (OIDC) round trip.</summary>
    public Task OnPostMicrosoftAsync()
    {
        var props = new AuthenticationProperties { RedirectUri = SafeReturnUrl() };
        return HttpContext.ChallengeAsync(AuthConstants.AzureAdScheme, props);
    }

    private string SafeReturnUrl()
    {
        return Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/";
    }

    internal async Task SignInAsAppUserAsync(User user)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.Name),
            new(AppClaims.Username, user.Username),
            new(AppClaims.TestUser, "true"),
        };
        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new(System.Security.Claims.ClaimTypes.Email, user.Email));
        if (!string.IsNullOrEmpty(user.ImageUrl))
            claims.Add(new(AppClaims.ImageUrl, user.ImageUrl));

        var identity = new System.Security.Claims.ClaimsIdentity(claims, AuthConstants.AppScheme);
        await HttpContext.SignInAsync(AuthConstants.AppScheme, new System.Security.Claims.ClaimsPrincipal(identity));
    }
}
