using Idasletten.Features.Tournaments.Entities;

namespace Idasletten.Features.Matches.Entities;

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
    public DateTimeOffset? PlayedAt { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamMatchResult> TeamResults { get; set; } = new List<TournamentTeamMatchResult>();
}
