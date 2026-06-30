using Idasletten.Features.Matches;
using Idasletten.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Shared.Scoring;

/// <summary>
/// Recomputes every TournamentPlayer's aggregate fields for a tournament by resetting them
/// to system defaults and replaying every Done match, in Order, through the tournament's
/// score calculator. Used both when a new result is recorded and when a Done match is
/// edited, so edits never need to "undo" a previous (possibly non-reversible) score update.
/// </summary>
public class ScoreRecalculator(IdaslettenDbContext db)
{
    public async Task RecalculateAsync(Guid tournamentId, CancellationToken ct = default)
    {
        var tournament = await db.Tournaments.FirstAsync(t => t.Id == tournamentId, ct);
        var players = await db.TournamentPlayers
            .Where(p => p.TournamentId == tournamentId)
            .ToListAsync(ct);

        var calculator = ScoreCalculatorFactory.Create(tournament.ScoreSystem);

        foreach (var player in players)
        {
            calculator.ResetPlayer(player);
            player.WinCount = 0;
            player.LoseCount = 0;
            player.MatchCount = 0;
            player.PointsWon = 0;
            player.PointsLost = 0;
            player.ScoreDiff = 0;
        }

        var matches = await db.TournamentMatches
            .Where(m => m.TournamentId == tournamentId && m.State == MatchState.Done)
            .Include(m => m.Teams).ThenInclude(t => t.Players)
            .OrderBy(m => m.Order)
            .ToListAsync(ct);

        var results = await db.TournamentTeamMatchResults
            .Where(r => r.TournamentId == tournamentId)
            .ToListAsync(ct);

        var playersById = players.ToDictionary(p => p.Id);

        foreach (var match in matches)
        {
            var teamOutcomes = match.Teams
                .Select(team =>
                {
                    var result = results.First(r => r.TeamId == team.Id);
                    var teamPlayers = team.Players
                        .Select(tp => playersById[tp.TournamentPlayerId])
                        .ToList();
                    return new TeamOutcome(team.Id, result.GoalsWon, result.GoalsLost, teamPlayers);
                })
                .ToList();

            var scoreBefore = teamOutcomes
                .SelectMany(t => t.Players)
                .ToDictionary(p => p.Id, p => p.Score);

            calculator.ApplyMatch(teamOutcomes);

            var bestNet = teamOutcomes.Max(t => t.NetGoals);
            foreach (var outcome in teamOutcomes)
            {
                var won = outcome.NetGoals == bestNet;
                foreach (var player in outcome.Players)
                {
                    player.MatchCount++;
                    player.PointsWon += outcome.GoalsWon;
                    player.PointsLost += outcome.GoalsLost;
                    if (won) player.WinCount++; else player.LoseCount++;
                    player.ScoreDiff = player.Score - scoreBefore[player.Id];
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
