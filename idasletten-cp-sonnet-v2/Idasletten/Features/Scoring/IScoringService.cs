using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public interface IScoringService
{
    void CalculateScores(
        List<TournamentPlayer> team1Players,
        List<TournamentPlayer> team2Players,
        int team1Goals,
        int team2Goals,
        Tournament tournament);
}
