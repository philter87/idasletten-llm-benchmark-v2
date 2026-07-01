namespace Idasletten.Shared.Entities;

public class TournamentPlayer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Value depends on the tournament's ScoreSystem.</summary>
    public double Score { get; set; }

    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }

    /// <summary>Only meaningful when ScoreSystem = Lives. Default 3.</summary>
    public int Lives { get; set; } = 3;

    /// <summary>Points is the same as goals; named generically to support other scoring systems / game types.</summary>
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }

    /// <summary>Change in Score since the player's last match. Display delta, e.g. +12 or -12.</summary>
    public double ScoreDiff { get; set; }
}
