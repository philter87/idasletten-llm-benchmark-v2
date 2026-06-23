namespace Idasletten.Features.Users.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty; // Usually 3 initials, unique
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Features.Tournaments.Entities.TournamentPlayer> TournamentPlayers { get; set; } = new List<Features.Tournaments.Entities.TournamentPlayer>();
}
