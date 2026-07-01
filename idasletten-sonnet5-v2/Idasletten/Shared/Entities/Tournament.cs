namespace Idasletten.Shared.Entities;

public class Tournament
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Name { get; set; } = string.Empty;
    public int TeamSize { get; set; } = 2;
    public int PointsToWin { get; set; } = 5;
    public ScoreSystem ScoreSystem { get; set; }
    public int? MaxPlayerCount { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }

    /// <summary>Source tournament used to seed/plan matches in this tournament.</summary>
    public Guid? SeedTournamentId { get; set; }

    /// <summary>
    /// This tournament is a later round that continues from a previous tournament;
    /// the parent's results are used to create this tournament's players.
    /// A tournament may be seeded only if it has no parent.
    /// </summary>
    public Guid? ParentTournamentId { get; set; }

    /// <summary>Only relevant when ParentTournamentId is set. Auto-incremented from the parent.</summary>
    public int? RoundNumber { get; set; }

    public List<TournamentPlayer> Players { get; set; } = [];
    public List<TournamentMatch> Matches { get; set; } = [];
}
