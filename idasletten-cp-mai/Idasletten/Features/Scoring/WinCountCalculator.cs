using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Scoring;

public class WinCountCalculator : IScoreCalculator
{
    public double InitialScore => 0;

    public void ApplyMatch(Tournament tournament, Dictionary<Guid, TournamentPlayer> playersByUserId, TournamentMatch match)
    {
        var teams = match.Teams.ToList();
        if (teams.Count < 2) return;

        if (teams.Count == 2)
        {
            var teamA = teams[0];
            var teamB = teams[1];
            bool aWins = teamA.GoalsWon > teamB.GoalsWon;
            bool bWins = teamB.GoalsWon > teamA.GoalsWon;
            bool draw = teamA.GoalsWon == teamB.GoalsWon;

            UpdateTeam(teamA, teamB.GoalsWon, aWins, draw, playersByUserId);
            UpdateTeam(teamB, teamA.GoalsWon, bWins, draw, playersByUserId);
        }
        else
        {
            int best = teams.Max(t => t.GoalsWon);
            foreach (var team in teams)
            {
                bool wins = team.GoalsWon == best;
                UpdateTeam(team, 0, wins, false, playersByUserId);
            }
        }
    }

    private static void UpdateTeam(TournamentTeam team, int opponentGoals, bool wins, bool draw,
        Dictionary<Guid, TournamentPlayer> playersByUserId)
    {
        foreach (var member in team.Members)
        {
            var player = playersByUserId[member.UserId];
            double before = player.Score;
            player.PointsWon += team.GoalsWon;
            player.PointsLost += opponentGoals;
            player.MatchCount++;
            if (wins) player.WinCount++;
            else if (!draw) player.LoseCount++;
            player.Score = player.WinCount;
            player.ScoreDiff = player.Score - before;
        }
    }
}
