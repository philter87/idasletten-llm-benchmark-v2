using Idasletten.Shared.Data;

namespace Idasletten.Shared.Scoring;

public static class ScoreCalculator
{
    public static void Reset(Tournament tournament)
    {
        foreach (var player in tournament.Players)
        {
            player.Score = tournament.ScoreSystem is ScoreSystem.Elo or ScoreSystem.TrueSkill ? 1000 : 0;
            player.WinCount = 0;
            player.MatchCount = 0;
            player.LoseCount = 0;
            player.Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 0;
            player.PointsWon = 0;
            player.PointsLost = 0;
            player.ScoreDiff = 0;
        }
    }

    public static void Apply(Tournament tournament, TournamentMatch match)
    {
        if (match.State != MatchState.Done || match.Results.Count < 2) return;
        var orderedResults = match.Results.OrderByDescending(result => result.GoalsWon).ThenBy(result => result.GoalsLost).ToList();
        var winnerTeamId = orderedResults[0].TeamId;
        var teams = match.Teams.ToDictionary(team => team.Id);
        var beforeScores = tournament.Players.ToDictionary(player => player.Id, player => player.Score);

        foreach (var result in orderedResults)
        {
            var isWinner = result.TeamId == winnerTeamId;
            foreach (var teamPlayer in teams[result.TeamId].Players)
            {
                var player = teamPlayer.Player;
                player.MatchCount++;
                player.PointsWon += result.GoalsWon;
                player.PointsLost += result.GoalsLost;
                if (isWinner) player.WinCount++;
                else
                {
                    player.LoseCount++;
                    if (tournament.ScoreSystem == ScoreSystem.Lives) player.Lives = Math.Max(0, player.Lives - 1);
                }
            }
        }

        switch (tournament.ScoreSystem)
        {
            case ScoreSystem.WinCount:
                foreach (var player in tournament.Players) player.Score = player.WinCount;
                break;
            case ScoreSystem.Lives:
                foreach (var player in tournament.Players) player.Score = player.Lives;
                break;
            case ScoreSystem.TrueSkill:
                ApplyEloLikeRating(match, teams, 24);
                break;
            default:
                ApplyEloLikeRating(match, teams, 32);
                break;
        }

        foreach (var player in tournament.Players) player.ScoreDiff = Math.Round(player.Score - beforeScores[player.Id], 1);
    }

    private static void ApplyEloLikeRating(TournamentMatch match, Dictionary<Guid, TournamentTeam> teams, double kFactor)
    {
        if (match.Results.Count != 2) return;
        var first = match.Results[0];
        var second = match.Results[1];
        var firstTeam = teams[first.TeamId];
        var secondTeam = teams[second.TeamId];
        var firstAverage = firstTeam.Players.Average(player => player.Player.Score);
        var secondAverage = secondTeam.Players.Average(player => player.Player.Score);
        var firstExpected = 1.0 / (1.0 + Math.Pow(10.0, (secondAverage - firstAverage) / 400.0));
        var firstActual = first.GoalsWon == second.GoalsWon ? 0.5 : first.GoalsWon > second.GoalsWon ? 1.0 : 0.0;
        var delta = kFactor * (firstActual - firstExpected);
        foreach (var teamPlayer in firstTeam.Players) teamPlayer.Player.Score = Math.Round(teamPlayer.Player.Score + delta, 1);
        foreach (var teamPlayer in secondTeam.Players) teamPlayer.Player.Score = Math.Round(teamPlayer.Player.Score - delta, 1);
    }
}
