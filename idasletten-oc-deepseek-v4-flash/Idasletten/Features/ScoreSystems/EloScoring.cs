using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;

namespace Idasletten.Features.ScoreSystems;

public class EloScoring : IScoringSystem
{
    public ScoreSystem Type => ScoreSystem.Elo;

    public void Calculate(List<Guid> team1PlayerIds, List<Guid> team2PlayerIds,
        int team1Score, int team2Score, List<TournamentPlayer> allPlayers)
    {
        var team1Players = allPlayers.Where(p => team1PlayerIds.Contains(p.UserId)).ToList();
        var team2Players = allPlayers.Where(p => team2PlayerIds.Contains(p.UserId)).ToList();

        double team1Avg = team1Players.Any() ? team1Players.Average(p => p.Score) : 1000;
        double team2Avg = team2Players.Any() ? team2Players.Average(p => p.Score) : 1000;

        double expected1 = 1.0 / (1.0 + Math.Pow(10, (team2Avg - team1Avg) / 400.0));
        double expected2 = 1.0 - expected1;

        double actual1 = team1Score > team2Score ? 1.0 : (team1Score == team2Score ? 0.5 : 0.0);
        double actual2 = 1.0 - actual1;

        const double kFactor = 32.0;
        double scoreChange1 = kFactor * (actual1 - expected1);
        double scoreChange2 = kFactor * (actual2 - expected2);

        foreach (var player in team1Players)
            player.Score += scoreChange1;
        foreach (var player in team2Players)
            player.Score += scoreChange2;
    }
}
