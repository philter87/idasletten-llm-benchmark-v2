namespace Idasletten.Shared.Domain;

/// <summary>A user's participation and standing within a single tournament.</summary>
public class TournamentPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();

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

    /// <summary>Points == goals won (named generically to support other game types).</summary>
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }

    /// <summary>Change in Score since the player's last match. Display delta, e.g. +12 or -12.</summary>
    public double ScoreDiff { get; set; }
}
