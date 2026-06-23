using Idasletten.Features.Matches.Entities;

namespace Idasletten.Features.Tournaments.Entities;

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
    public string Name { get; set; } = string.Empty;
    public int TeamSize { get; set; } = 2;
    public int PointsToWin { get; set; } = 5;
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
    public int? MaxPlayerCount { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }
    public Guid? SeedTournamentId { get; set; }
    public Guid? ParentTournamentId { get; set; }
    public int? RoundNumber { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tournament? SeedTournament { get; set; }
    public Tournament? ParentTournament { get; set; }
    public ICollection<Tournament> ChildTournaments { get; set; } = new List<Tournament>();

    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
}
