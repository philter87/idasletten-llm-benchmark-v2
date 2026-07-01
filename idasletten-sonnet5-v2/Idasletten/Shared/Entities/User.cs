using Microsoft.AspNetCore.Identity;

namespace Idasletten.Shared.Entities;

/// <summary>
/// A tournament participant. Inherits the standard ASP.NET Identity field set
/// (Id, UserName, Email, ...) even though authentication itself is federated
/// via Azure AD / the test-user scheme rather than local passwords.
/// </summary>
public class User : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Fetched from Azure Graph API when the user is created.</summary>
    public string? ImageUrl { get; set; }

    public List<TournamentPlayer> TournamentPlayers { get; set; } = [];
}
