using Idasletten.Shared.Entities;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Recomputes every TournamentPlayer's Score/WinCount/MatchCount/LoseCount/Lives/PointsWon/
/// PointsLost/ScoreDiff from scratch by replaying all Done matches in order. Running a full
/// recompute (rather than an incremental undo/redo) keeps every scoring system correct even
/// when an already-completed match is edited out of order.
///
/// Requires tournament.Players, tournament.Matches (with Teams.TeamPlayers.TournamentPlayer
/// and Results) to already be loaded.
/// </summary>
public static class TournamentScoreRecalculator
{
    public static void Recalculate(Tournament tournament)
    {
        var strategy = ScoreSystemStrategyFactory.Create(tournament.ScoreSystem);

        foreach (var player in tournament.Players)
        {
            player.Score = strategy.InitialScore;
            player.WinCount = 0;
            player.MatchCount = 0;
            player.LoseCount = 0;
            player.Lives = 3;
            player.PointsWon = 0;
            player.PointsLost = 0;
            player.ScoreDiff = 0;
        }

        var doneMatches = tournament.Matches
            .Where(m => m.State == MatchState.Done)
            .OrderBy(m => m.Order);

        foreach (var match in doneMatches)
        {
            var resultsByTeam = match.Results.ToDictionary(r => r.TeamId);
            var teamInfos = match.Teams
                .Select(t => new TeamMatchInfo
                {
                    TeamId = t.Id,
                    Players = t.TeamPlayers.Select(tp => tp.TournamentPlayer).ToList(),
                    GoalsWon = resultsByTeam.TryGetValue(t.Id, out var result) ? result.GoalsWon : 0,
                    GoalsLost = resultsByTeam.TryGetValue(t.Id, out var result2) ? result2.GoalsLost : 0,
                })
                .ToList();

            if (teamInfos.Count == 0)
            {
                continue;
            }

            var maxGoals = teamInfos.Max(t => t.GoalsWon);
            var isSoleWinner = teamInfos.Count(t => t.GoalsWon == maxGoals) == 1;

            var scoresBefore = teamInfos.SelectMany(t => t.Players).Select(p => (Player: p, Before: p.Score)).ToList();

            foreach (var team in teamInfos)
            {
                var isTop = team.GoalsWon == maxGoals;
                foreach (var player in team.Players)
                {
                    player.PointsWon += team.GoalsWon;
                    player.PointsLost += team.GoalsLost;
                    player.MatchCount++;
                    if (isTop && isSoleWinner)
                    {
                        player.WinCount++;
                    }
                    else if (!isTop)
                    {
                        player.LoseCount++;
                    }
                }
            }

            strategy.ApplyMatch(teamInfos);

            foreach (var (player, before) in scoresBefore)
            {
                player.ScoreDiff = player.Score - before;
            }
        }
    }
}
