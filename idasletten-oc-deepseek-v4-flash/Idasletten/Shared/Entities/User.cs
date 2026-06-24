using Microsoft.AspNetCore.Identity;

namespace Idasletten.Shared.Entities;

public class User : IdentityUser<Guid>
{
    public string Initials { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
}
