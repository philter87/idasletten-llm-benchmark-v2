namespace Idasletten.Features.Scoring;

/// <summary>
/// Standard Elo rating system. For multi-player teams, uses the average Elo of the team.
/// K-factor of 32.
/// </summary>
public class EloScoreCalculator : IScoreCalculator
{
    private const double DefaultScore = 1000.0;
    private const double KFactor = 32.0;

    public IReadOnlyList<PlayerScoreUpdate> CalculateScores(
        IReadOnlyList<PlayerMatchResult> results,
        IReadOnlyDictionary<Guid, double> currentScores)
    {
        var updates = new List<PlayerScoreUpdate>();

        // Split into winners and losers
        var winners = results.Where(r => r.Won).ToList();
        var losers = results.Where(r => !r.Won).ToList();

        if (winners.Count == 0 || losers.Count == 0)
            return updates;

        double winnerAvgElo = winners.Average(r => currentScores.GetValueOrDefault(r.PlayerId, DefaultScore));
        double loserAvgElo = losers.Average(r => currentScores.GetValueOrDefault(r.PlayerId, DefaultScore));

        double expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loserAvgElo - winnerAvgElo) / 400.0));
        double expectedLoser = 1.0 - expectedWinner;

        foreach (var winner in winners)
        {
            double current = currentScores.GetValueOrDefault(winner.PlayerId, DefaultScore);
            double newScore = current + KFactor * (1.0 - expectedWinner);
            updates.Add(new PlayerScoreUpdate(winner.PlayerId, newScore, newScore - current));
        }

        foreach (var loser in losers)
        {
            double current = currentScores.GetValueOrDefault(loser.PlayerId, DefaultScore);
            double newScore = current + KFactor * (0.0 - expectedLoser);
            updates.Add(new PlayerScoreUpdate(loser.PlayerId, newScore, newScore - current));
        }

        return updates;
    }
}
