using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public class ScoringServiceFactory
{
    public static IScoringService Create(ScoreSystem scoreSystem) =>
        scoreSystem switch
        {
            ScoreSystem.Elo => new EloScoringService(),
            ScoreSystem.TrueSkill => new TrueSkillScoringService(),
            ScoreSystem.Lives => new LivesScoringService(),
            ScoreSystem.WinCount => new WinCountScoringService(),
            _ => throw new ArgumentOutOfRangeException(nameof(scoreSystem), scoreSystem, null)
        };
}
