using Microsoft.AspNetCore.Identity;

namespace Idasletten.Shared.Data.Entities;

public class User : IdentityUser
{
    /// <summary>
    /// Usually 3 initials. This should be unique.
    /// </summary>
    public override string UserName { get; set; } = default!;
    
    /// <summary>
    /// Full name of the user
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Email address (optional, as user may not have email)
    /// </summary>
    public override string? Email { get; set; }
    
    /// <summary>
    /// URL to user's image, fetched via Azure Graph API
    /// </summary>
    public string? ImageUrl { get; set; }
    
    // Navigation properties
    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
}
