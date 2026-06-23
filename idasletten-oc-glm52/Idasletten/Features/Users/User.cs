using Idasletten.Features.Players;

namespace Idasletten.Features.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ImageUrl { get; set; }
    public string? AzureObjectId { get; set; }
    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
}