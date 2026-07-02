namespace Idasletten.Features.Tournaments;

public enum ScoreSystem
{
    Elo,
    TrueSkill,
    Lives,
    WinCount
}

public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";

    /// <summary>Players per team.</summary>
    public int TeamSize { get; set; } = 2;

    /// <summary>Points (goals) needed to win a match.</summary>
    public int PointsToWin { get; set; } = 5;

    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

    /// <summary>Empty means no limit to the number of players.</summary>
    public int? MaxPlayerCount { get; set; }

    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }

    /// <summary>Source tournament used to seed/plan matches in this tournament.</summary>
    public Guid? SeedTournamentId { get; set; }

    /// <summary>
    /// This tournament is a later round continuing from a previous tournament.
    /// A tournament may be seeded only if it has no parent.
    /// </summary>
    public Guid? ParentTournamentId { get; set; }

    /// <summary>Only relevant when ParentTournamentId is set. Starts at 1.</summary>
    public int? RoundNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TournamentPlayer> Players { get; set; } = [];
    public List<TournamentTeam> Teams { get; set; } = [];
}
