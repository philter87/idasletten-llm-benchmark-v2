using Idasletten.Shared.Data.Entities;

namespace Idasletten.Shared.Scoring;

public interface ILivesScoringSystem : IScoringSystem
{
    int InitialLives { get; set; }
}
