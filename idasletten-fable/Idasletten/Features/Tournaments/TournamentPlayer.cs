using Idasletten.Features.Users;

namespace Idasletten.Features.Tournaments;

public class TournamentPlayer
{
    public const int DefaultLives = 3;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TournamentId { get; set; }

    /// <summary>Value depends on the tournament's ScoreSystem.</summary>
    public double Score { get; set; }

    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }

    /// <summary>Only meaningful when ScoreSystem = Lives.</summary>
    public int Lives { get; set; }

    /// <summary>Points = goals. Named to support other scoring systems or game types.</summary>
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }

    /// <summary>Change in Score since the player's last match (display delta, e.g. +12 / -12).</summary>
    public double ScoreDiff { get; set; }

    /// <summary>TrueSkill needs both mean and standard deviation persisted between matches.</summary>
    public double TrueSkillMean { get; set; }
    public double TrueSkillStdDev { get; set; }

    public User User { get; set; } = null!;
}
