using Idasletten.Features.Matches;
using Idasletten.Features.Players;

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
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TeamSize { get; set; } = 2;
    public int PointsToWin { get; set; } = 5;
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
    public int? MaxPlayerCount { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }
    public Guid? SeedTournamentId { get; set; }
    public Tournament? SeedTournament { get; set; }
    public Guid? ParentTournamentId { get; set; }
    public Tournament? ParentTournament { get; set; }
    public int RoundNumber { get; set; } = 1;

    public ICollection<TournamentPlayer> Players { get; set; } = [];
    public ICollection<TournamentMatch> Matches { get; set; } = [];
}
