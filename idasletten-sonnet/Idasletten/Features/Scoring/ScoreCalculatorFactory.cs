using Idasletten.Shared.Enums;

namespace Idasletten.Features.Scoring;

public class ScoreCalculatorFactory(IServiceProvider services)
{
    public IScoreCalculator GetCalculator(ScoreSystem scoreSystem) => scoreSystem switch
    {
        ScoreSystem.Elo => services.GetRequiredService<EloScoreCalculator>(),
        ScoreSystem.TrueSkill => services.GetRequiredService<TrueSkillScoreCalculator>(),
        ScoreSystem.Lives => services.GetRequiredService<LivesScoreCalculator>(),
        ScoreSystem.WinCount => services.GetRequiredService<WinCountScoreCalculator>(),
        _ => throw new ArgumentOutOfRangeException(nameof(scoreSystem))
    };
}
