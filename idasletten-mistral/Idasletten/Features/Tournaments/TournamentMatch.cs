using Idasletten.Shared;

namespace Idasletten.Features.Tournaments;

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Order { get; set; }
    public Guid TournamentId { get; set; }
    public MatchState State { get; set; } = MatchState.Planned;

    // Navigation properties
    public virtual Tournament Tournament { get; set; } = null!;
    public virtual ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
    public virtual ICollection<TournamentTeamMatchResult> Results { get; set; } = new List<TournamentTeamMatchResult>();
}
