using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Idasletten.Shared.Auth;

public static class AuthExtensions
{
    public const string TestScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static bool TestLoginEnabled(IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration["TestUser:Email"]) && !string.IsNullOrWhiteSpace(configuration["TestUser:Password"]);
    public static bool AzureLoginEnabled(IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration["AzureAd:ClientId"]) && !string.IsNullOrWhiteSpace(configuration["AzureAd:TenantId"]);

    public static async Task SignInTestUserAsync(HttpContext httpContext, string email)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, email), new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Name, "Test User") };
        await httpContext.SignInAsync(TestScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, TestScheme)));
    }
}
