using Idasletten.Features.Tournaments;

namespace Idasletten.Shared.Scoring;

public static class ScoreCalculatorFactory
{
    public static IScoreCalculator Create(ScoreSystem system) => system switch
    {
        ScoreSystem.Elo => new EloScoreCalculator(),
        ScoreSystem.TrueSkill => new TrueSkillScoreCalculator(),
        ScoreSystem.Lives => new LivesScoreCalculator(),
        ScoreSystem.WinCount => new WinCountScoreCalculator(),
        _ => throw new ArgumentOutOfRangeException(nameof(system), system, null)
    };
}
