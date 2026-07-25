using System.Security.Claims;

namespace Idasletten.Shared.Auth;

public static class AuthConstants
{
    public const string AzureAdScheme = "AzureAd";

    /// <summary>Claim holding the id of the matching row in our own user table.</summary>
    public const string AppUserIdClaim = "idasletten:user-id";

    /// <summary>Claim holding the initials the user is known by in the tournaments.</summary>
    public const string InitialsClaim = "idasletten:initials";

    public static Guid? GetAppUserId(this ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirstValue(AppUserIdClaim);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string? GetInitials(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(InitialsClaim);
}
