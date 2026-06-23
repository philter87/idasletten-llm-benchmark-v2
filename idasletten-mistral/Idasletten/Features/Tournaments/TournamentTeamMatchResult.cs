namespace Idasletten.Features.Tournaments;

public class TournamentTeamMatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MatchId { get; set; }
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    public int GoalsWon { get; set; }
    public int GoalsLost { get; set; }

    // Navigation properties
    public virtual TournamentMatch Match { get; set; } = null!;
    public virtual Tournament Tournament { get; set; } = null!;
    public virtual TournamentTeam Team { get; set; } = null!;
}
