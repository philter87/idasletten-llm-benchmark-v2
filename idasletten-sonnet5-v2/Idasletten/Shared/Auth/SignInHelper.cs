using System.Security.Claims;
using Idasletten.Shared.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Idasletten.Shared.Auth;

public static class SignInHelper
{
    public static Task SignInAsync(HttpContext httpContext, User user)
    {
        var claims = new List<Claim>
        {
            new(AuthConstants.UserIdClaimType, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
        };
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        return httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    public static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(AuthConstants.UserIdClaimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
