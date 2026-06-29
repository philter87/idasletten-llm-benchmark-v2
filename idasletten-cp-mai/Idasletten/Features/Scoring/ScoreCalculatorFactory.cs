using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Scoring;

public interface IScoreCalculatorFactory
{
    IScoreCalculator Create(ScoreSystem system);
}

public class ScoreCalculatorFactory : IScoreCalculatorFactory
{
    public IScoreCalculator Create(ScoreSystem system) => system switch
    {
        ScoreSystem.Elo => new EloCalculator(),
        ScoreSystem.TrueSkill => new TrueSkillCalculatorWrapper(),
        ScoreSystem.Lives => new LivesCalculator(),
        ScoreSystem.WinCount => new WinCountCalculator(),
        _ => new EloCalculator()
    };
}
