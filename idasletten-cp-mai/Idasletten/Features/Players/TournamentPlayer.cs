namespace Idasletten.Features.Players;

public class TournamentPlayer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Users.AppUser User { get; set; } = null!;
    public Guid TournamentId { get; set; }
    public Tournaments.Tournament Tournament { get; set; } = null!;

    public double Score { get; set; }
    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }
    public int Lives { get; set; } = 3;
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }
    public double ScoreDiff { get; set; }

    // TrueSkill state
    public double TrueSkillMean { get; set; } = 25;
    public double TrueSkillStdDev { get; set; } = 25.0 / 3.0;
}
