namespace Idasletten.Shared.Entities;

public enum ScoreSystem
{
    Elo,
    TrueSkill,
    Lives,
    WinCount
}

public class Tournament
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int TeamSize { get; set; } = 2;
    public int PointsToWin { get; set; } = 5;
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
    public int? MaxPlayerCount { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }
    public Guid? SeedTournamentId { get; set; }
    public Guid? ParentTournamentId { get; set; }
    public int? RoundNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tournament? SeedTournament { get; set; }
    public Tournament? ParentTournament { get; set; }
    public ICollection<TournamentPlayer> Players { get; set; } = [];
    public ICollection<TournamentMatch> Matches { get; set; } = [];
    public ICollection<TournamentTeam> Teams { get; set; } = [];
}
