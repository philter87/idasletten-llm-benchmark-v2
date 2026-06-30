namespace Idasletten.Features.Matches;

public class TournamentTeamMatchResult
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }
}
