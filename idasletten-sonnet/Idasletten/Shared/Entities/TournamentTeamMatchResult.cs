namespace Idasletten.Shared.Entities;

public class TournamentTeamMatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MatchId { get; set; }
    public TournamentMatch Match { get; set; } = null!;
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    public TournamentTeam Team { get; set; } = null!;
    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }
}
