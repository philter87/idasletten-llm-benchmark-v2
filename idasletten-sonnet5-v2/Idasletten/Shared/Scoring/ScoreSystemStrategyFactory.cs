using Idasletten.Shared.Entities;

namespace Idasletten.Shared.Scoring;

public static class ScoreSystemStrategyFactory
{
    public static IScoreSystemStrategy Create(ScoreSystem system) => system switch
    {
        ScoreSystem.Elo => new EloScoreSystemStrategy(),
        ScoreSystem.TrueSkill => new TrueSkillScoreSystemStrategy(),
        ScoreSystem.Lives => new LivesScoreSystemStrategy(),
        ScoreSystem.WinCount => new WinCountScoreSystemStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(system), system, "Unknown score system"),
    };
}
