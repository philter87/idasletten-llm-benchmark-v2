using Idasletten.Features.Matches;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Users;

namespace Idasletten.Features.Players;

/// <summary>A user's participation in one tournament, including all their stats in it.</summary>
public class TournamentPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Value depends on the tournament's <see cref="ScoreSystem"/>.</summary>
    public double Score { get; set; }

    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }

    /// <summary>Only meaningful when the tournament uses the Lives score system.</summary>
    public int Lives { get; set; }

    /// <summary>Points is the same as goals - named this way to support other game types later.</summary>
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }

    /// <summary>Change in <see cref="Score"/> caused by the player's last match. Displayed as +12 / -12.</summary>
    public double ScoreDiff { get; set; }

    /// <summary>TrueSkill mean (mu). Only used when the tournament uses TrueSkill.</summary>
    public double SkillMean { get; set; }

    /// <summary>TrueSkill standard deviation (sigma). Only used when the tournament uses TrueSkill.</summary>
    public double SkillDeviation { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<TournamentTeamPlayer> TeamMemberships { get; set; } = [];

    public int PointsDiff => PointsWon - PointsLost;

    public int DrawCount => MatchCount - WinCount - LoseCount;

    /// <summary>A player is knocked out when playing for lives and all of them are gone.</summary>
    public bool IsKnockedOut(ScoreSystem system) => system == ScoreSystem.Lives && Lives <= 0;
}
