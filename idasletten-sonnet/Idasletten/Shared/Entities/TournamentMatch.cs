using Idasletten.Shared.Enums;

namespace Idasletten.Shared.Entities;

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Order { get; set; }
    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;
    public MatchState State { get; set; } = MatchState.Planned;
    public DateTime? PlayedAt { get; set; }

    public ICollection<TournamentTeamMatchResult> TeamResults { get; set; } = new List<TournamentTeamMatchResult>();
}
