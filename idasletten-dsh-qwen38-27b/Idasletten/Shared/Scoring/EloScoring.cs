using Idasletten.Models;

namespace Idasletten.Scoring;

/// <summary>
/// Normal Elo. Base rating 1500, K=32. A multi-player team's rating is the
/// average of its members' ratings. Every ordered pair of teams in the match
/// contributes K*(S - E) to each member of the better-scoring team and the
/// symmetric value to the other team (league-style multi-player Elo).
/// </summary>
public sealed class EloScoring : IScoringEngine
{
    public const double BaseRating = 1500;
    public const double KFactor = 32;

    public ScoreSystem System => ScoreSystem.Elo;
    public double InitialScore => BaseRating;

    public void Initialize(TournamentPlayer player)
    {
        player.Score = BaseRating;
        player.ScoreDiff = 0;
        player.TrueSkillSigma = 0;
    }

    public void Apply(TournamentPlayer[] players, int goals, IReadOnlyList<TeamResult> allTeams)
    {
        var self = allTeams.First(t => t.Players.Any(p => players.Any(q => ReferenceEquals(q, p))));
        double selfAvg = self.Players.Average(p => p.Score);

        double delta = 0;
        foreach (var other in allTeams.Where(t => !ReferenceEquals(t, self)))
        {
            double otherAvg = other.Players.Average(p => p.Score);
            double expected = 1.0 / (1.0 + Math.Pow(10, (otherAvg - selfAvg) / 400.0));
            double s = self.Goals > other.Goals ? 1.0 : self.Goals < other.Goals ? 0.0 : 0.5;
            delta += KFactor * (s - expected);
        }

        foreach (var p in players)
        {
            p.Score += delta;
            p.ScoreDiff = delta;
        }
    }
}
