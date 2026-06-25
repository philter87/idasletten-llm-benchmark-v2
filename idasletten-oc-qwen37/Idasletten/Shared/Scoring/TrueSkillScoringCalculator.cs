using Idasletten.Models;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;

namespace Idasletten.Shared.Scoring;

public class TrueSkillScoringCalculator : IScoringCalculator
{
    private const double INITIAL_MU = 25.0;
    private const double INITIAL_SIGMA = 25.0 / 3.0;

    public void CalculateScores(Tournament tournament, TournamentMatch match)
    {
        var completedResults = match.TeamResults.ToList();
        if (completedResults.Count < 2) return;

        var gameInfo = new GameInfo(INITIAL_MU, INITIAL_SIGMA, INITIAL_MU / 2, INITIAL_SIGMA / 10, 0.03);

        var sortedResults = completedResults.OrderByDescending(r => r.GoalsWon).ToList();

        var teams = new List<IDictionary<Player, Rating>>();
        var teamPlayers = new List<List<TournamentPlayer>>();
        var ranks = new List<int>();

        for (int i = 0; i < sortedResults.Count; i++)
        {
            var result = sortedResults[i];
            var players = result.Team.Players.ToList();
            var team = new Dictionary<Player, Rating>();

            foreach (var player in players)
            {
                var mu = player.Score == 0 ? INITIAL_MU : player.Score;
                var sigma = INITIAL_SIGMA;
                team.Add(new Player(player.Id.ToString()), new Rating(mu, sigma));
            }

            teams.Add(team);
            teamPlayers.Add(players);
            ranks.Add(i);
        }

        var newRatings = TrueSkillCalculator.CalculateNewRatings(gameInfo, teams, ranks.ToArray());

        for (int i = 0; i < sortedResults.Count; i++)
        {
            var result = sortedResults[i];
            var players = teamPlayers[i];
            var isWinner = i == 0;

            foreach (var player in players)
            {
                var playerId = new Player(player.Id.ToString());
                var newRating = newRatings[playerId];
                var oldScore = player.Score == 0 ? INITIAL_MU : player.Score;
                player.ScoreDiff = newRating.Mean - oldScore;
                player.Score = newRating.Mean;

                if (isWinner)
                {
                    player.WinCount++;
                }
                else
                {
                    player.LoseCount++;
                }

                player.PointsWon += result.GoalsWon;
                player.PointsLost += result.GoalsLost;
                player.MatchCount++;
            }
        }
    }
}
