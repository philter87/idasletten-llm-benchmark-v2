namespace Idasletten.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Unique short handle, usually 3 initials (e.g. "THO").</summary>
    public string Username { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Optional — a user may not have an email address.</summary>
    public string? Email { get; set; }

    /// <summary>Fetched via the Azure Graph API when the user is created (if configured).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>PBKDF2 hash; only set for the seeded test user.</summary>
    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
}
