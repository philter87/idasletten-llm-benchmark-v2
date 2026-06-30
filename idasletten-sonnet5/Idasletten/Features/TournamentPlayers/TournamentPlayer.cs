namespace Idasletten.Features.TournamentPlayers;

public class TournamentPlayer
{
    public Guid Id { get; set; }
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
}
