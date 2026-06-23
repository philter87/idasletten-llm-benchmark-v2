using Idasletten.Shared;

namespace Idasletten.Features.Tournaments;

public class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int TeamSize { get; set; } = 2;
    public int PointsToWin { get; set; } = 5;
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
    public int? MaxPlayerCount { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; } = true;
    public Guid? SeedTournamentId { get; set; }
    public Guid? ParentTournamentId { get; set; }
    public int? RoundNumber { get; set; } = 1;

    // Navigation properties
    public virtual Tournament? SeedTournament { get; set; }
    public virtual Tournament? ParentTournament { get; set; }
    public virtual ICollection<Tournament> ChildTournaments { get; set; } = new List<Tournament>();
    public virtual ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public virtual ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public virtual ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
}
