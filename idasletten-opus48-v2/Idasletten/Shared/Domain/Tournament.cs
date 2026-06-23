namespace Idasletten.Shared.Domain;

public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    /// <summary>Players per team. Default 2.</summary>
    public int TeamSize { get; set; } = 2;

    /// <summary>Points (goals) needed to win a match. Default 5.</summary>
    public int PointsToWin { get; set; } = 5;

    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

    /// <summary>Optional max number of players. Null means unlimited.</summary>
    public int? MaxPlayerCount { get; set; }

    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }

    /// <summary>Source tournament used to seed/plan matches in this tournament.</summary>
    public Guid? SeedTournamentId { get; set; }
    public Tournament? SeedTournament { get; set; }

    /// <summary>
    /// When set, this tournament is a later round that continues from a previous tournament.
    /// A tournament may be seeded only if it has no parent.
    /// </summary>
    public Guid? ParentTournamentId { get; set; }
    public Tournament? ParentTournament { get; set; }

    /// <summary>Only relevant when ParentTournamentId is set. Default 1, auto-incremented from parent.</summary>
    public int? RoundNumber { get; set; }

    public List<TournamentPlayer> Players { get; set; } = new();
    public List<TournamentTeam> Teams { get; set; } = new();
    public List<TournamentMatch> Matches { get; set; } = new();
}
