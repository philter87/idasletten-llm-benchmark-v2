using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public static class ScoringServiceFactory
{
    public static IScoringService Create(ScoreSystem system) => system switch
    {
        ScoreSystem.Elo => new EloScoringService(),
        ScoreSystem.TrueSkill => new TrueSkillScoringService(),
        ScoreSystem.Lives => new LivesScoringService(),
        ScoreSystem.WinCount => new WinCountScoringService(),
        _ => throw new ArgumentOutOfRangeException(nameof(system), system, "Unknown scoring system")
    };
}
