using Idasletten.Shared.Data.Entities;

namespace Idasletten.Shared.Scoring;

public interface ITrueSkillScoringSystem : IScoringSystem
{
    double Mu { get; }
    double Sigma { get; }
}
