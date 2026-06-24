using Idasletten.Shared.Enums;

namespace Idasletten.Shared.Entities;

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

    public Tournament? SeedTournament { get; set; }
    public Tournament? ParentTournament { get; set; }
    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
}
