using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public class EloScoringService : IScoringService
{
    private const double K = 32;

    public void Calculate(
        TournamentMatch match,
        IReadOnlyList<TournamentTeamMatchResult> teamResults,
        IReadOnlyList<TournamentTeam> teams,
        IReadOnlyList<TournamentPlayer> players)
    {
        var team1Result = teamResults[0];
        var team2Result = teamResults[1];
        var team1 = teams.Single(t => t.Id == team1Result.TeamId);
        var team2 = teams.Single(t => t.Id == team2Result.TeamId);

        var team1Players = GetPlayers(team1, players);
        var team2Players = GetPlayers(team2, players);

        var team1AvgScore = team1Players.Average(p => p.Score);
        var team2AvgScore = team2Players.Average(p => p.Score);

        var team1Expected = 1.0 / (1.0 + Math.Pow(10, (team2AvgScore - team1AvgScore) / 400.0));
        var team2Expected = 1.0 - team1Expected;

        var team1Won = team1Result.GoalsWon > team2Result.GoalsWon;
        var team1Actual = team1Won ? 1.0 : 0.0;
        var team2Actual = 1.0 - team1Actual;

        var scoreChange1 = K * (team1Actual - team1Expected);
        var scoreChange2 = K * (team2Actual - team2Expected);

        foreach (var p in team1Players)
        {
            p.ScoreDiff = scoreChange1;
            p.Score += scoreChange1;
        }

        foreach (var p in team2Players)
        {
            p.ScoreDiff = scoreChange2;
            p.Score += scoreChange2;
        }
    }

    private static List<TournamentPlayer> GetPlayers(TournamentTeam team, IReadOnlyList<TournamentPlayer> allPlayers)
    {
        return team.TeamPlayers
            .Select(tp => allPlayers.Single(p => p.Id == tp.TournamentPlayerId))
            .ToList();
    }
}
