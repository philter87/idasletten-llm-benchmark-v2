using Idasletten.Models;

namespace Idasletten.Shared.Scoring;

public class ScoringCalculatorFactory
{
    public static IScoringCalculator GetCalculator(ScoreSystem scoreSystem)
    {
        return scoreSystem switch
        {
            ScoreSystem.Elo => new EloScoringCalculator(),
            ScoreSystem.TrueSkill => new TrueSkillScoringCalculator(),
            ScoreSystem.Lives => new LivesScoringCalculator(),
            ScoreSystem.WinCount => new WinCountScoringCalculator(),
            _ => throw new ArgumentException($"Unknown score system: {scoreSystem}")
        };
    }
}
