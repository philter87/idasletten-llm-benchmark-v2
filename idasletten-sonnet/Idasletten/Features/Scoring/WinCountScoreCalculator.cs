using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public class WinCountScoreCalculator : IScoreCalculator
{
    public void UpdateScores(IList<TournamentPlayer> team1Players, IList<TournamentPlayer> team2Players,
        int team1Goals, int team2Goals, Tournament tournament)
    {
        foreach (var p in team1Players)
        {
            p.MatchCount++;
            p.PointsWon += team1Goals;
            p.PointsLost += team2Goals;
            if (team1Goals > team2Goals)
            {
                p.WinCount++;
                p.ScoreDiff = 1;
            }
            else if (team1Goals < team2Goals)
            {
                p.LoseCount++;
                p.ScoreDiff = 0;
            }
            else
            {
                p.ScoreDiff = 0;
            }
            p.Score = p.WinCount;
        }

        foreach (var p in team2Players)
        {
            p.MatchCount++;
            p.PointsWon += team2Goals;
            p.PointsLost += team1Goals;
            if (team2Goals > team1Goals)
            {
                p.WinCount++;
                p.ScoreDiff = 1;
            }
            else if (team2Goals < team1Goals)
            {
                p.LoseCount++;
                p.ScoreDiff = 0;
            }
            else
            {
                p.ScoreDiff = 0;
            }
            p.Score = p.WinCount;
        }
    }
}
