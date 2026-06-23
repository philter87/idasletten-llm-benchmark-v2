namespace Idasletten.Features.Scoring;

/// <summary>
/// Lives scoring system: losing a match costs 1 life.
/// Score represents remaining lives. Players with more lives rank higher.
/// </summary>
public class LivesScoreCalculator : IScoreCalculator
{
    public IReadOnlyList<PlayerScoreUpdate> CalculateScores(
        IReadOnlyList<PlayerMatchResult> results,
        IReadOnlyDictionary<Guid, double> currentScores)
    {
        var updates = new List<PlayerScoreUpdate>();

        foreach (var result in results)
        {
            double current = currentScores.GetValueOrDefault(result.PlayerId, 3.0);
            if (!result.Won)
            {
                double newScore = Math.Max(0, current - 1);
                updates.Add(new PlayerScoreUpdate(result.PlayerId, newScore, newScore - current));
            }
            else
            {
                // Winners keep their lives; no change, still record no-op
                updates.Add(new PlayerScoreUpdate(result.PlayerId, current, 0));
            }
        }

        return updates;
    }
}
