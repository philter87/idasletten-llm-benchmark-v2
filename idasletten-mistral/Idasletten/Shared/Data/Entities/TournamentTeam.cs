namespace Idasletten.Shared.Data.Entities;

public class TournamentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// This is not an input field, but auto-generated Team 1, Team 2 etc.
    /// Maybe overridden in a future feature.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// This is not an input field, but auto-generated 1, 2 etc.
    /// </summary>
    public int Number { get; set; }
    
    public Guid TournamentId { get; set; }
    
    // Navigation properties
    public Tournament Tournament { get; set; } = default!;
    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentTeamMatchResult> MatchResults { get; set; } = new List<TournamentTeamMatchResult>();
}
