using Idasletten.Shared.Entities;

namespace Idasletten.Features.Scoring;

public class LivesScoringService : IScoringService
{
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

        var team1Won = team1Result.GoalsWon > team2Result.GoalsWon;

        foreach (var p in team1Players)
        {
            if (!team1Won && p.Lives > 0)
            {
                p.ScoreDiff = -1;
                p.Score -= 1;
                p.Lives--;
            }
            else
            {
                p.ScoreDiff = 0;
            }
        }

        foreach (var p in team2Players)
        {
            if (team1Won && p.Lives > 0)
            {
                p.ScoreDiff = -1;
                p.Score -= 1;
                p.Lives--;
            }
            else
            {
                p.ScoreDiff = 0;
            }
        }
    }

    private static List<TournamentPlayer> GetPlayers(TournamentTeam team, IReadOnlyList<TournamentPlayer> allPlayers)
    {
        return team.TeamPlayers
            .Select(tp => allPlayers.Single(p => p.Id == tp.TournamentPlayerId))
            .ToList();
    }
}
