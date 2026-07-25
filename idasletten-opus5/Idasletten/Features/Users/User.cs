using Idasletten.Features.Players;
using Microsoft.AspNetCore.Identity;

namespace Idasletten.Features.Users;

/// <summary>
/// A person that can join many tournaments. Built on the standard ASP.NET Identity user so that
/// UserName (the 3 initials), NormalizedUserName, Email, SecurityStamp etc. come for free.
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>Full name, e.g. "Odin Alfadir". Optional - initials are the only required input.</summary>
    public string? Name { get; set; }

    /// <summary>Profile picture fetched from the Microsoft Graph API when the user is created.</summary>
    public string? ImageUrl { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<TournamentPlayer> TournamentPlayers { get; set; } = [];

    /// <summary>The initials the user is known by. Alias of the Identity UserName field.</summary>
    public string Initials => UserName ?? string.Empty;

    /// <summary>Name if we have one, otherwise the initials.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Initials : Name;
}
