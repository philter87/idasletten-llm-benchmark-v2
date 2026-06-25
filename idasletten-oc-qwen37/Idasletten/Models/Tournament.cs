using System.ComponentModel.DataAnnotations;

namespace Idasletten.Models;

public class Tournament
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int TeamSize { get; set; } = 2;

    public int PointsToWin { get; set; } = 5;

    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

    public int? MaxPlayerCount { get; set; }

    public bool IsArchived { get; set; } = false;

    public bool IsPublic { get; set; } = true;

    public Guid? SeedTournamentId { get; set; }
    public Tournament? SeedTournament { get; set; }

    public Guid? ParentTournamentId { get; set; }
    public Tournament? ParentTournament { get; set; }

    public int RoundNumber { get; set; } = 1;

    public ICollection<Tournament> ChildTournaments { get; set; } = new List<Tournament>();

    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
}
