using Idasletten.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches;

/// Shared by the planned/recent/all-matches queries to build a human-readable label
/// ("ABC+DEF vs GHI") and score summary ("5-2") for a batch of matches.
public static class MatchSummaryBuilder
{
    public static async Task<List<MatchSummaryDto>> BuildAsync(
        IdaslettenDbContext db, List<TournamentMatch> matches, CancellationToken cancellationToken)
    {
        if (matches.Count == 0) return [];

        var matchIds = matches.Select(m => m.Id).ToList();

        var teams = await db.TournamentTeams
            .Where(t => matchIds.Contains(t.MatchId))
            .Include(t => t.Players)
            .ToListAsync(cancellationToken);

        var results = await db.TournamentTeamMatchResults
            .Where(r => matchIds.Contains(r.MatchId))
            .ToListAsync(cancellationToken);

        var tournamentPlayerIds = teams.SelectMany(t => t.Players).Select(p => p.TournamentPlayerId).ToList();
        var usernamesByTournamentPlayerId = await (
            from p in db.TournamentPlayers
            join u in db.Users on p.UserId equals u.Id
            where tournamentPlayerIds.Contains(p.Id)
            select new { p.Id, Username = u.UserName! }
        ).ToDictionaryAsync(x => x.Id, x => x.Username, cancellationToken);

        return matches.Select(match =>
        {
            var matchTeams = teams.Where(t => t.MatchId == match.Id).OrderBy(t => t.Number).ToList();

            var label = matchTeams.Count == 0
                ? "(no players yet)"
                : string.Join(" vs ", matchTeams.Select(t =>
                    string.Join("+", t.Players.Select(p => usernamesByTournamentPlayerId.GetValueOrDefault(p.TournamentPlayerId, "?")))));

            string? scoreLabel = null;
            if (match.State == MatchState.Done)
            {
                var matchResults = results.Where(r => r.MatchId == match.Id).ToList();
                scoreLabel = string.Join("-", matchTeams.Select(t =>
                    matchResults.FirstOrDefault(r => r.TeamId == t.Id)?.GoalsWon.ToString() ?? "0"));
            }

            return new MatchSummaryDto(match.Id, match.Order, match.State, label, scoreLabel);
        }).ToList();
    }
}
