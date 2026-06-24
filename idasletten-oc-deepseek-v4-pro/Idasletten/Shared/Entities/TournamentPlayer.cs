namespace Idasletten.Shared.Entities;

public class TournamentPlayer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TournamentId { get; set; }
    public double Score { get; set; } = 1000;
    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }
    public int Lives { get; set; } = 3;
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }
    public double ScoreDiff { get; set; }

    public User User { get; set; } = null!;
    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentTeamPlayer> TeamPlayers { get; set; } = [];
}
