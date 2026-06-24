namespace Idasletten.Shared.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = [];
}
