namespace Idasletten.Features.Scoring;

public record PlayerMatchResult(Guid PlayerId, int GoalsWon, int GoalsLost, bool Won);

public interface IScoreCalculator
{
    IReadOnlyList<PlayerScoreUpdate> CalculateScores(
        IReadOnlyList<PlayerMatchResult> results,
        IReadOnlyDictionary<Guid, double> currentScores);
}

public record PlayerScoreUpdate(Guid PlayerId, double NewScore, double ScoreDiff);
