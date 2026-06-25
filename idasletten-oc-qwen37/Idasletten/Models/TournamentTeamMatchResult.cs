namespace Idasletten.Models;

public class TournamentTeamMatchResult
{
    public Guid Id { get; set; }

    public Guid MatchId { get; set; }
    public TournamentMatch Match { get; set; } = null!;

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public Guid TeamId { get; set; }
    public TournamentTeam Team { get; set; } = null!;

    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }
}
