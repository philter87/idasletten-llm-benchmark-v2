using Idasletten.Features.Users;

namespace Idasletten.Features.Tournaments;

public class TournamentPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TournamentId { get; set; }
    public double Score { get; set; } = 0;
    public int WinCount { get; set; } = 0;
    public int MatchCount { get; set; } = 0;
    public int LoseCount { get; set; } = 0;
    public int Lives { get; set; } = 3;
    public int PointsWon { get; set; } = 0;
    public int PointsLost { get; set; } = 0;
    public double ScoreDiff { get; set; } = 0;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Tournament Tournament { get; set; } = null!;
    public virtual ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
}
