using System.Security.Claims;
using Idasletten.Features.Users;
using Idasletten.Shared.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Auth;

/// <summary>
/// Finds or creates the local User for a successful Azure AD sign-in, then replaces the OIDC
/// principal with one carrying our own User.Id as the NameIdentifier so the rest of the app
/// doesn't need to know whether someone signed in via Azure AD or the test-user login.
/// </summary>
public static class AzureAdSignInHandler
{
    public static async Task HandleTokenValidatedAsync(TokenValidatedContext context)
    {
        var principal = context.Principal ?? throw new InvalidOperationException("Missing principal.");
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("preferred_username");
        var name = principal.FindFirstValue("name") ?? email ?? "Unknown";

        var services = context.HttpContext.RequestServices;
        var db = services.GetRequiredService<IdaslettenDbContext>();

        var normalizedEmail = email?.ToUpperInvariant();
        var user = normalizedEmail is null
            ? null
            : await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (user is null)
        {
            var username = await GenerateUniqueUsernameAsync(db, email, name);
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                Name = name,
                Email = email,
                NormalizedEmail = normalizedEmail,
                EmailConfirmed = email is not null,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var accessToken = context.TokenEndpointResponse?.AccessToken;
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                var avatarFetcher = services.GetRequiredService<GraphAvatarFetcher>();
                user.ImageUrl = await avatarFetcher.TryFetchAndSaveAsync(accessToken, user.Id);
                await db.SaveChangesAsync();
            }
        }

        var identity = new ClaimsIdentity(context.Scheme.Name);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
        if (user.Email is not null)
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        }
        context.Principal = new ClaimsPrincipal(identity);
    }

    private static async Task<string> GenerateUniqueUsernameAsync(IdaslettenDbContext db, string? email, string name)
    {
        var baseUsername = (email?.Split('@').FirstOrDefault() ?? name).Replace(" ", "").ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(baseUsername))
        {
            baseUsername = "user";
        }

        var candidate = baseUsername;
        var suffix = 1;
        while (await db.Users.AnyAsync(u => u.NormalizedUserName == candidate.ToUpperInvariant()))
        {
            candidate = $"{baseUsername}{suffix++}";
        }
        return candidate;
    }
}
