using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public interface IScoreCalculator
{
    void UpdateScores(IList<TournamentPlayer> team1Players, IList<TournamentPlayer> team2Players,
        int team1Goals, int team2Goals, Tournament tournament);
}
