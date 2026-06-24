namespace Idasletten.Shared.Data.Entities;

public class TournamentPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string UserId { get; set; } = default!;
    public Guid TournamentId { get; set; }
    
    /// <summary>
    /// Value depends on the tournament's ScoreSystem.
    /// </summary>
    public double Score { get; set; } = 0;
    
    public int WinCount { get; set; } = 0;
    public int MatchCount { get; set; } = 0;
    public int LoseCount { get; set; } = 0;
    
    /// <summary>
    /// Only meaningful when ScoreSystem = Lives. Should only be set when the tournament uses the Lives scoring system. Default is 3.
    /// </summary>
    public int Lives { get; set; } = 3;
    
    /// <summary>
    /// Points is the same as goals. This naming is selected to support other scoring systems or game types.
    /// </summary>
    public int PointsWon { get; set; } = 0;
    public int PointsLost { get; set; } = 0;
    
    /// <summary>
    /// Change in Score since the player's last match. Display delta. Ex +12 or -12.
    /// </summary>
    public double ScoreDiff { get; set; } = 0;
    
    // Navigation properties
    public User User { get; set; } = default!;
    public Tournament Tournament { get; set; } = default!;
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
}
