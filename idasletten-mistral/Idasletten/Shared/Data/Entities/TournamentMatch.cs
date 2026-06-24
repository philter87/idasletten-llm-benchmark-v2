using Idasletten.Shared.Data.Enums;

namespace Idasletten.Shared.Data.Entities;

public class TournamentMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Sequence/display ordering.
    /// </summary>
    public int Order { get; set; }
    
    public Guid TournamentId { get; set; }
    
    public MatchState State { get; set; } = MatchState.Planned;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public Tournament Tournament { get; set; } = default!;
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
    public ICollection<TournamentTeamMatchResult> Results { get; set; } = new List<TournamentTeamMatchResult>();
}
