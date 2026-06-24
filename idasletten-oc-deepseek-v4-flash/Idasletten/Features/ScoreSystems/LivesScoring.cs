using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;

namespace Idasletten.Features.ScoreSystems;

public class LivesScoring : IScoringSystem
{
    public ScoreSystem Type => ScoreSystem.Lives;

    public void Calculate(List<Guid> team1PlayerIds, List<Guid> team2PlayerIds,
        int team1Score, int team2Score, List<TournamentPlayer> allPlayers)
    {
        // Lives are reduced in RecordMatchResultHandler based on losing
        // Score is represented as remaining lives
        foreach (var player in allPlayers)
        {
            player.Score = player.Lives;
        }
    }
}
