using Idasletten.Models;

namespace Idasletten.Shared.Scoring;

public class EloScoringCalculator : IScoringCalculator
{
    private const int K_FACTOR = 32;
    private const double INITIAL_RATING = 1500;

    public void CalculateScores(Tournament tournament, TournamentMatch match)
    {
        var completedResults = match.TeamResults.ToList();
        if (completedResults.Count < 2) return;

        var winner = completedResults.OrderByDescending(r => r.GoalsWon).First();
        var losers = completedResults.Where(r => r.Id != winner.Id).ToList();

        foreach (var loser in losers)
        {
            var winnerTeamPlayers = winner.Team.Players.ToList();
            var loserTeamPlayers = loser.Team.Players.ToList();

            var winnerAvgRating = winnerTeamPlayers.Any()
                ? winnerTeamPlayers.Average(p => p.Score == 0 ? INITIAL_RATING : p.Score)
                : INITIAL_RATING;

            var loserAvgRating = loserTeamPlayers.Any()
                ? loserTeamPlayers.Average(p => p.Score == 0 ? INITIAL_RATING : p.Score)
                : INITIAL_RATING;

            var expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loserAvgRating - winnerAvgRating) / 400.0));
            var expectedLoser = 1.0 - expectedWinner;

            var winnerChange = K_FACTOR * (1.0 - expectedWinner);
            var loserChange = K_FACTOR * (0.0 - expectedLoser);

            foreach (var player in winnerTeamPlayers)
            {
                player.ScoreDiff = winnerChange;
                player.Score += winnerChange;
                player.WinCount++;
                player.PointsWon += winner.GoalsWon;
                player.PointsLost += winner.GoalsLost;
                player.MatchCount++;
            }

            foreach (var player in loserTeamPlayers)
            {
                player.ScoreDiff = loserChange;
                player.Score += loserChange;
                player.LoseCount++;
                player.PointsWon += loser.GoalsWon;
                player.PointsLost += loser.GoalsLost;
                player.MatchCount++;
            }
        }
    }
}
