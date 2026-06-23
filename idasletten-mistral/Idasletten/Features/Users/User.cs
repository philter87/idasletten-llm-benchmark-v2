using Microsoft.AspNetCore.Identity;

namespace Idasletten.Features.Users;

public class User : IdentityUser<Guid>
{
    public string Username { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ImageUrl { get; set; }

    // Navigation properties
    public virtual ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
}
