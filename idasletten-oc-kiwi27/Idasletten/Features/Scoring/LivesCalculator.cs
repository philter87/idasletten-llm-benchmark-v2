using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Scoring;

public class LivesCalculator : IScoreCalculator
{
    public const int InitialLives = 3;
    public double InitialScore => InitialLives;

    public void ApplyMatch(Tournament tournament, Dictionary<Guid, TournamentPlayer> playersByUserId, TournamentMatch match)
    {
        var teams = match.Teams.ToList();
        if (teams.Count < 2) return;

        // For the common 2-team case, the losing team loses a life.
        if (teams.Count == 2)
        {
            var teamA = teams[0];
            var teamB = teams[1];
            bool aWins = teamA.GoalsWon > teamB.GoalsWon;
            bool bWins = teamB.GoalsWon > teamA.GoalsWon;

            UpdateTeam(teamA, teamB.GoalsWon, !aWins && bWins, playersByUserId);
            UpdateTeam(teamB, teamA.GoalsWon, !bWins && aWins, playersByUserId);
        }
        else
        {
            int best = teams.Max(t => t.GoalsWon);
            foreach (var team in teams)
            {
                bool lost = team.GoalsWon < best;
                UpdateTeam(team, 0, lost, playersByUserId);
            }
        }
    }

    private static void UpdateTeam(TournamentTeam team, int opponentGoals, bool lost,
        Dictionary<Guid, TournamentPlayer> playersByUserId)
    {
        foreach (var member in team.Members)
        {
            var player = playersByUserId[member.UserId];
            double before = player.Score;
            player.PointsWon += team.GoalsWon;
            player.PointsLost += opponentGoals;
            player.MatchCount++;
            if (lost)
            {
                player.LoseCount++;
                player.Lives--;
            }
            else
            {
                player.WinCount++;
            }
            player.Score = player.Lives;
            player.ScoreDiff = player.Score - before;
        }
    }
}
