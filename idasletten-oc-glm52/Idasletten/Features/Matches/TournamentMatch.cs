using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Matches;

public enum MatchState
{
    Planned,
    Done,
    Cancelled
}

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TournamentId { get; set; }
    public int Order { get; set; }
    public MatchState State { get; set; } = MatchState.Planned;
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();

    public Tournament Tournament { get; set; } = null!;
}

public class TournamentTeamMatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MatchId { get; set; }
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }

    public TournamentMatch Match { get; set; } = null!;
    public TournamentTeam Team { get; set; } = null!;
}