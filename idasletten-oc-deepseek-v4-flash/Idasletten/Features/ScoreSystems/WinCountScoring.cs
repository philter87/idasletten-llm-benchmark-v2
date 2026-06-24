using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;

namespace Idasletten.Features.ScoreSystems;

public class WinCountScoring : IScoringSystem
{
    public ScoreSystem Type => ScoreSystem.WinCount;

    public void Calculate(List<Guid> team1PlayerIds, List<Guid> team2PlayerIds,
        int team1Score, int team2Score, List<TournamentPlayer> allPlayers)
    {
        // Score = WinCount is handled in RecordMatchResultHandler
        // This ensures consistency
        foreach (var player in allPlayers)
        {
            player.Score = player.WinCount;
        }
    }
}
