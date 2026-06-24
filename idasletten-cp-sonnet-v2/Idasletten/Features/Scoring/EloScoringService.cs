using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

/// <summary>
/// Standard Elo scoring. For multi-player teams, average the team Elo scores.
/// </summary>
public class EloScoringService : IScoringService
{
    private const double K = 32;

    public void CalculateScores(
        List<TournamentPlayer> team1Players,
        List<TournamentPlayer> team2Players,
        int team1Goals,
        int team2Goals,
        Tournament tournament)
    {
        double team1Elo = team1Players.Average(p => p.Score);
        double team2Elo = team2Players.Average(p => p.Score);

        double expected1 = 1.0 / (1.0 + Math.Pow(10, (team2Elo - team1Elo) / 400));
        double expected2 = 1.0 - expected1;

        double actual1 = team1Goals > team2Goals ? 1.0 : (team1Goals == team2Goals ? 0.5 : 0.0);
        double actual2 = 1.0 - actual1;

        double delta1 = K * (actual1 - expected1);
        double delta2 = K * (actual2 - expected2);

        bool team1Won = team1Goals > team2Goals;
        bool team2Won = team2Goals > team1Goals;

        foreach (var player in team1Players)
        {
            player.ScoreDiff = delta1;
            player.Score += delta1;
            player.MatchCount++;
            player.PointsWon += team1Goals;
            player.PointsLost += team2Goals;
            if (team1Won) player.WinCount++;
            else if (team2Won) player.LoseCount++;
        }

        foreach (var player in team2Players)
        {
            player.ScoreDiff = delta2;
            player.Score += delta2;
            player.MatchCount++;
            player.PointsWon += team2Goals;
            player.PointsLost += team1Goals;
            if (team2Won) player.WinCount++;
            else if (team1Won) player.LoseCount++;
        }
    }
}
