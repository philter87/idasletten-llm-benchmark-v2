using Idasletten.Shared.Enums;

namespace Idasletten.Features.ScoreSystems;

public interface IScoringSystem
{
    ScoreSystem Type { get; }
    void Calculate(List<Guid> team1PlayerIds, List<Guid> team2PlayerIds,
        int team1Score, int team2Score, List<Shared.Entities.TournamentPlayer> allPlayers);
}
