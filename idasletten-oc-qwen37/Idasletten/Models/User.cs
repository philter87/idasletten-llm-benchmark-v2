using System.ComponentModel.DataAnnotations;

namespace Idasletten.Models;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
}
