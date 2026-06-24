using Idasletten.Shared.Data.Entities;

namespace Idasletten.Shared.Scoring;

public interface IScoringSystem
{
    Task CalculateMatchResultsAsync(TournamentMatch match, Tournament tournament);
    Task UpdatePlayerScoresAsync(TournamentPlayer player, TournamentMatch match, Tournament tournament);
}
