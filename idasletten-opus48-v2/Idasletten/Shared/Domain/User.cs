using Microsoft.AspNetCore.Identity;

namespace Idasletten.Shared.Domain;

/// <summary>
/// A person who can play in tournaments. Extends the standard .NET Identity user
/// so we reuse as many built-in identity fields as possible (Id, UserName, Email, ...).
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>Display name, e.g. "Thor Odinson".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Profile image fetched from the Azure Graph API when the user is created.</summary>
    public string? ImageUrl { get; set; }

    public List<TournamentPlayer> TournamentPlayers { get; set; } = new();
}
