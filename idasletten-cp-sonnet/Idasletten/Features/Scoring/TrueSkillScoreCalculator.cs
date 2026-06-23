using Moserware.Skills;

namespace Idasletten.Features.Scoring;

/// <summary>
/// TrueSkill rating system using Moserware.Skills library.
/// Score is stored as (mean - 3 * standardDeviation) * 100 for display purposes.
/// </summary>
public class TrueSkillScoreCalculator : IScoreCalculator
{
    private const double DefaultMean = 25.0;
    private const double DefaultStdDev = 8.333;

    public IReadOnlyList<PlayerScoreUpdate> CalculateScores(
        IReadOnlyList<PlayerMatchResult> results,
        IReadOnlyDictionary<Guid, double> currentScores)
    {
        var updates = new List<PlayerScoreUpdate>();

        var winners = results.Where(r => r.Won).ToList();
        var losers = results.Where(r => !r.Won).ToList();

        if (winners.Count == 0 || losers.Count == 0)
        {
            return updates;
        }

        var winnerTeam = new Team<Guid>();
        var loserTeam = new Team<Guid>();

        foreach (var winner in winners)
        {
            var (mean, deviation) = DecomposeScore(currentScores.GetValueOrDefault(winner.PlayerId, ToScore(DefaultMean, DefaultStdDev)));
            winnerTeam.AddPlayer(winner.PlayerId, new Rating(mean, deviation));
        }

        foreach (var loser in losers)
        {
            var (mean, deviation) = DecomposeScore(currentScores.GetValueOrDefault(loser.PlayerId, ToScore(DefaultMean, DefaultStdDev)));
            loserTeam.AddPlayer(loser.PlayerId, new Rating(mean, deviation));
        }

        var updatedRatings = TrueSkillCalculator.CalculateNewRatings(GameInfo.DefaultGameInfo, Teams.Concat(winnerTeam, loserTeam), 1, 2);

        foreach (var rating in updatedRatings)
        {
            var playerId = rating.Key;
            var oldScore = currentScores.GetValueOrDefault(playerId, ToScore(DefaultMean, DefaultStdDev));
            var newScore = ToScore(rating.Value.Mean, rating.Value.StandardDeviation);
            updates.Add(new PlayerScoreUpdate(playerId, newScore, newScore - oldScore));
        }

        return updates;
    }

    private static double ToScore(double mean, double deviation) =>
        (mean - 3.0 * deviation) * 100.0;

    private static (double mean, double deviation) DecomposeScore(double score)
    {
        var mean = score / 100.0 + 3.0 * DefaultStdDev;
        return (mean, DefaultStdDev);
    }
}
