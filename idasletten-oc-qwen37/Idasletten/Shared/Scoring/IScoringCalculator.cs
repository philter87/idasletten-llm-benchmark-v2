using Idasletten.Models;

namespace Idasletten.Shared.Scoring;

public interface IScoringCalculator
{
    void CalculateScores(Tournament tournament, TournamentMatch match);
}
