namespace Idasletten.Features.Matches;

public class TournamentMatch
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public Guid TournamentId { get; set; }
    public MatchState State { get; set; } = MatchState.Planned;

    public List<TournamentTeam> Teams { get; set; } = [];
}
