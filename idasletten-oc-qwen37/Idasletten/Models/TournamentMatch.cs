namespace Idasletten.Models;

public class TournamentMatch
{
    public Guid Id { get; set; }

    public int Order { get; set; }

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public MatchState State { get; set; } = MatchState.Planned;

    public ICollection<TournamentTeamMatchResult> TeamResults { get; set; } = new List<TournamentTeamMatchResult>();
}
