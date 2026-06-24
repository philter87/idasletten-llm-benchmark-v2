namespace Idasletten.Shared.Data.Entities;

public class TournamentTeamMatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid MatchId { get; set; }
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    
    public int GoalsWon { get; set; } = 0;
    public int GoalsLost { get; set; } = 0;
    
    // Navigation properties
    public TournamentMatch Match { get; set; } = default!;
    public Tournament Tournament { get; set; } = default!;
    public TournamentTeam Team { get; set; } = default!;
}
