using Idasletten.Shared.Data.Enums;

namespace Idasletten.Shared.Scoring;

public interface IScoringSystemFactory
{
    IScoringSystem GetScoringSystem(ScoreSystem scoreSystem);
}
