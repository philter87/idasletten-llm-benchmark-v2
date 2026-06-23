using Idasletten.Features.Teams;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;

namespace Idasletten.Features.Players;

public class TournamentPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TournamentId { get; set; }
    public double Score { get; set; }
    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }
    public int Lives { get; set; } = 3;
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }
    public double ScoreDiff { get; set; }

    public User User { get; set; } = null!;
    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
}