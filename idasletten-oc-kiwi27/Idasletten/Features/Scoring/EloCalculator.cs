using Idasletten.Features.Matches;
using Idasletten.Features.Players;
using Idasletten.Features.Tournaments;

namespace Idasletten.Features.Scoring;

public class EloCalculator : IScoreCalculator
{
    public const double InitialRating = 1500;
    public const double K = 32;

    public double InitialScore => InitialRating;

    public void ApplyMatch(Tournament tournament, Dictionary<Guid, TournamentPlayer> playersByUserId, TournamentMatch match)
    {
        var teams = match.Teams.ToList();
        if (teams.Count < 2) return;

        // For now support two-team matches
        if (teams.Count != 2) return;

        var teamA = teams[0];
        var teamB = teams[1];

        var membersA = teamA.Members.ToList();
        var membersB = teamB.Members.ToList();

        double avgA = membersA.Count > 0 ? membersA.Average(m => playersByUserId[m.UserId].Score) : InitialRating;
        double avgB = membersB.Count > 0 ? membersB.Average(m => playersByUserId[m.UserId].Score) : InitialRating;

        double expectedA = 1.0 / (1.0 + Math.Pow(10, (avgB - avgA) / 400.0));
        double expectedB = 1.0 - expectedA;

        double scoreA, scoreB;
        if (teamA.GoalsWon > teamB.GoalsWon) { scoreA = 1; scoreB = 0; }
        else if (teamA.GoalsWon < teamB.GoalsWon) { scoreA = 0; scoreB = 1; }
        else { scoreA = 0.5; scoreB = 0.5; }

        UpdateTeam(teamA, membersA, scoreA, expectedA, teamB.GoalsWon, playersByUserId);
        UpdateTeam(teamB, membersB, scoreB, expectedB, teamA.GoalsWon, playersByUserId);
    }

    private static void UpdateTeam(TournamentTeam team, List<TournamentPlayer> members, double actual, double expected, int opponentGoals,
        Dictionary<Guid, TournamentPlayer> playersByUserId)
    {
        double delta = K * (actual - expected);
        foreach (var member in members)
        {
            var player = playersByUserId[member.UserId];
            double before = player.Score;
            player.Score = before + delta;
            player.ScoreDiff = player.Score - before;
            player.PointsWon += team.GoalsWon;
            player.PointsLost += opponentGoals;
            player.MatchCount++;
            if (actual == 1) player.WinCount++;
            else if (actual == 0) player.LoseCount++;
        }
    }
}
