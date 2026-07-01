using System.Security.Claims;
using Idasletten.Data;
using Idasletten.Features.Users.Commands.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Auth;

/// <summary>
/// On successful Azure AD sign-in, finds or creates the matching domain User and
/// stamps its id onto the principal so the rest of the app only ever deals with User.Id.
/// </summary>
public static class AzureAdUserProvisioning
{
    public static async Task OnTokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null)
        {
            return;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("preferred_username")
            ?? principal.FindFirstValue(ClaimTypes.Upn);
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? email ?? "Unknown";

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<IdaslettenDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            var sender = context.HttpContext.RequestServices.GetRequiredService<ISender>();
            var initials = UsernameGenerator.DeriveInitials(name);
            user = await sender.Send(new CreateUserCommand(initials, name, email));

            // Best-effort: fetch the user's photo from the organization's Azure AD via Graph API.
            // Requires the Graph "User.Read" (or similar) scope to be consented in the app registration.
            await GraphProfilePhotoFetcher.TryFetchAndSetAsync(context.HttpContext, db, user);
        }

        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim(AuthConstants.UserIdClaimType, user.Id.ToString()));
    }
}
