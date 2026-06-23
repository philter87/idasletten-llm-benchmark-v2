using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public class EloScoreCalculator : IScoreCalculator
{
    private const int K = 32;

    public void UpdateScores(IList<TournamentPlayer> team1Players, IList<TournamentPlayer> team2Players,
        int team1Goals, int team2Goals, Tournament tournament)
    {
        double team1Avg = team1Players.Average(p => p.Score);
        double team2Avg = team2Players.Average(p => p.Score);

        double expected1 = 1.0 / (1.0 + Math.Pow(10, (team2Avg - team1Avg) / 400.0));
        double expected2 = 1.0 - expected1;

        double actual1 = team1Goals > team2Goals ? 1.0 : team1Goals == team2Goals ? 0.5 : 0.0;
        double actual2 = 1.0 - actual1;

        double delta1 = K * (actual1 - expected1);
        double delta2 = K * (actual2 - expected2);

        foreach (var p in team1Players)
        {
            p.ScoreDiff = delta1;
            p.Score += delta1;
            p.MatchCount++;
            p.PointsWon += team1Goals;
            p.PointsLost += team2Goals;
            if (team1Goals > team2Goals) p.WinCount++;
            else if (team1Goals < team2Goals) p.LoseCount++;
        }

        foreach (var p in team2Players)
        {
            p.ScoreDiff = delta2;
            p.Score += delta2;
            p.MatchCount++;
            p.PointsWon += team2Goals;
            p.PointsLost += team1Goals;
            if (team2Goals > team1Goals) p.WinCount++;
            else if (team2Goals < team1Goals) p.LoseCount++;
        }
    }
}
