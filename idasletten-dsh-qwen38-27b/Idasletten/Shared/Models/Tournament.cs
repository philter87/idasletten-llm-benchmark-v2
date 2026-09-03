namespace Idasletten.Models;

public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;

    /// <summary>Players per team. Default 2.</summary>
    public int TeamSize { get; set; } = 2;

    /// <summary>Goals needed to win a match. Default 5.</summary>
    public int PointsToWin { get; set; } = 5;

    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

    /// <summary>Optional cap on the number of players. Null = unlimited.</summary>
    public int? MaxPlayerCount { get; set; }

    public bool IsArchived { get; set; }

    public bool IsPublic { get; set; }

    /// <summary>Source tournament used to seed/plan matches in this tournament.</summary>
    public Guid? SeedTournamentId { get; set; }
    public Tournament? SeedTournament { get; set; }

    /// <summary>Set when this tournament is a later round continuing from a previous tournament.</summary>
    public Guid? ParentTournamentId { get; set; }
    public Tournament? ParentTournament { get; set; }

    /// <summary>Only relevant when <see cref="ParentTournamentId"/> is set. Defaults to 1, auto-incremented for children.</summary>
    public int? RoundNumber { get; set; }

    /// <summary>When the tournament was created (ordering aid).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
    public ICollection<Tournament> ChildTournaments { get; set; } = new List<Tournament>();
}
