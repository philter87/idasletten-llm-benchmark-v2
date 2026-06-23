using Idasletten.Data;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches;

/// <summary>Loads a tournament's matches into <see cref="MatchView"/>s (teams, players, goals).</summary>
public static class MatchProjection
{
    public static async Task<List<MatchView>> LoadAsync(AppDbContext db, Guid tournamentId, CancellationToken ct)
    {
        var matches = await db.TournamentMatches
            .Where(m => m.TournamentId == tournamentId)
            .Include(m => m.Results).ThenInclude(r => r.Team).ThenInclude(t => t.Players).ThenInclude(p => p.User)
            .OrderBy(m => m.Order)
            .ToListAsync(ct);

        return matches.Select(m => new MatchView(
            m.Id, m.Order, m.State,
            m.Results
                .OrderBy(r => r.Team.Number)
                .Select(r => new TeamView(
                    r.TeamId,
                    r.Team.Name,
                    r.Team.Players.Select(p => p.User.UserName!).OrderBy(x => x).ToList(),
                    r.GoalsWon))
                .ToList()))
            .ToList();
    }

    public static async Task<MatchView?> LoadOneAsync(AppDbContext db, Guid matchId, CancellationToken ct)
    {
        var m = await db.TournamentMatches
            .Where(x => x.Id == matchId)
            .Include(x => x.Results).ThenInclude(r => r.Team).ThenInclude(t => t.Players).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(ct);
        if (m is null) return null;

        return new MatchView(
            m.Id, m.Order, m.State,
            m.Results
                .OrderBy(r => r.Team.Number)
                .Select(r => new TeamView(
                    r.TeamId,
                    r.Team.Name,
                    r.Team.Players.Select(p => p.User.UserName!).OrderBy(x => x).ToList(),
                    r.GoalsWon))
                .ToList());
    }
}
