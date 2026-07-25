using Idasletten.Features.Matches;
using Idasletten.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Scoring;

/// <summary>
/// Loads a whole tournament and recalculates every player's score from the played matches.
/// Command handlers call this after any change to a match, so editing an old result is always correct.
/// </summary>
public static class TournamentScoring
{
    public static async Task RecalculateAsync(
        AppDbContext db, Guid tournamentId, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);

        if (tournament is null)
        {
            return;
        }

        var matches = await db.TournamentMatches
            .Where(m => m.TournamentId == tournamentId && m.State == MatchState.Done)
            .Include(m => m.Results)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.Players)
            .OrderBy(m => m.Order)
            .ThenBy(m => m.CreatedUtc)
            .ToListAsync(cancellationToken);

        var playersById = tournament.Players.ToDictionary(p => p.Id);

        var played = matches.Select(match => new PlayedMatch(
            match.Results
                .Select(result => new TeamOutcome(
                    result.Team.Players
                        .Where(tp => playersById.ContainsKey(tp.TournamentPlayerId))
                        .Select(tp => playersById[tp.TournamentPlayerId])
                        .ToList(),
                    result.GoalsWon,
                    result.GoalsLost))
                .ToList()))
            .ToList();

        ScoreEngine.Recalculate(tournament, tournament.Players, played);

        await db.SaveChangesAsync(cancellationToken);
    }
}
