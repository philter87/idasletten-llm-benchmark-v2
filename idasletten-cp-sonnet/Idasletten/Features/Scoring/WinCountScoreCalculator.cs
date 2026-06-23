namespace Idasletten.Features.Scoring;

/// <summary>
/// WinCount scoring system. Score = number of wins.
/// Tie-breaker: goal difference (PointsWon - PointsLost) tracked separately on TournamentPlayer.
/// </summary>
public class WinCountScoreCalculator : IScoreCalculator
{
    public IReadOnlyList<PlayerScoreUpdate> CalculateScores(
        IReadOnlyList<PlayerMatchResult> results,
        IReadOnlyDictionary<Guid, double> currentScores)
    {
        var updates = new List<PlayerScoreUpdate>();

        foreach (var result in results)
        {
            double current = currentScores.GetValueOrDefault(result.PlayerId, 0.0);
            double newScore = result.Won ? current + 1 : current;
            updates.Add(new PlayerScoreUpdate(result.PlayerId, newScore, newScore - current));
        }

        return updates;
    }
}
