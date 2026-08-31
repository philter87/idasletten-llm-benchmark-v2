namespace Idasletten.Models;

public class TournamentPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Value depends on the tournament's ScoreSystem (Elo points, TrueSkill mu, remaining lives, or wins).</summary>
    public double Score { get; set; }

    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }

    /// <summary>Only meaningful when ScoreSystem == Lives (3 to start); 0 for other systems.</summary>
    public int Lives { get; set; }

    /// <summary>Points is the same as goals. Named for other scoring systems / game types.</summary>
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }

    /// <summary>Change in Score since the player's last match (display delta, e.g. +12 or -12).</summary>
    public double ScoreDiff { get; set; }

    /// <summary>TrueSkill sigma; only maintained for TrueSkill tournaments.</summary>
    public double TrueSkillSigma { get; set; }
}
