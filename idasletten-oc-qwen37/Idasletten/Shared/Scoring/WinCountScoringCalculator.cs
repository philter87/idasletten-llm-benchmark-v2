using Idasletten.Models;

namespace Idasletten.Shared.Scoring;

public class WinCountScoringCalculator : IScoringCalculator
{
    public void CalculateScores(Tournament tournament, TournamentMatch match)
    {
        var completedResults = match.TeamResults.ToList();
        if (completedResults.Count < 2) return;

        var winner = completedResults.OrderByDescending(r => r.GoalsWon).First();
        var losers = completedResults.Where(r => r.Id != winner.Id).ToList();

        foreach (var player in winner.Team.Players)
        {
            player.WinCount++;
            player.Score = player.WinCount;
            player.PointsWon += winner.GoalsWon;
            player.PointsLost += winner.GoalsLost;
            player.MatchCount++;
        }

        foreach (var loser in losers)
        {
            foreach (var player in loser.Team.Players)
            {
                player.LoseCount++;
                player.Score = player.WinCount;
                player.PointsWon += loser.GoalsWon;
                player.PointsLost += loser.GoalsLost;
                player.MatchCount++;
            }
        }
    }
}
