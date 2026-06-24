using Idasletten.Shared.Data.Entities;

namespace Idasletten.Shared.Scoring;

public interface IEloScoringSystem : IScoringSystem
{
    int KFactor { get; set; }
}
