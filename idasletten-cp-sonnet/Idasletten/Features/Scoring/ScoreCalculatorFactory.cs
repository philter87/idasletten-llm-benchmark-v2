using Idasletten.Features.Tournaments.Entities;

namespace Idasletten.Features.Scoring;

public class ScoreCalculatorFactory
{
    public static IScoreCalculator GetCalculator(ScoreSystem scoreSystem) => scoreSystem switch
    {
        ScoreSystem.Elo => new EloScoreCalculator(),
        ScoreSystem.TrueSkill => new TrueSkillScoreCalculator(),
        ScoreSystem.Lives => new LivesScoreCalculator(),
        ScoreSystem.WinCount => new WinCountScoreCalculator(),
        _ => throw new ArgumentOutOfRangeException(nameof(scoreSystem), scoreSystem, null)
    };
}
