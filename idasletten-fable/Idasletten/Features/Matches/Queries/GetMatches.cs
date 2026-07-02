using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record GetMatchesQuery(Guid TournamentId) : IRequest<MatchesResult?>;

public record MatchesResult(
    Tournament Tournament,
    List<TournamentMatch> Planned,
    List<TournamentMatch> Done,
    List<Tournament> AvailableSeedTournaments);

public class GetMatchesHandler(AppDbContext db) : IRequestHandler<GetMatchesQuery, MatchesResult?>
{
    public async Task<MatchesResult?> Handle(GetMatchesQuery request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, ct);
        if (tournament is null)
            return null;

        var matches = await db.TournamentMatches.AsNoTracking()
            .Include(m => m.Results)
            .ThenInclude(r => r.Team)
            .ThenInclude(t => t.Players)
            .ThenInclude(p => p.User)
            .Where(m => m.TournamentId == tournament.Id)
            .ToListAsync(ct);

        var availableSeeds = await db.Tournaments.AsNoTracking()
            .Where(t => t.Id != tournament.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return new MatchesResult(
            tournament,
            matches.Where(m => m.State == MatchState.Planned).OrderBy(m => m.Order).ToList(),
            matches.Where(m => m.State == MatchState.Done).OrderByDescending(m => m.PlayedAt).ToList(),
            availableSeeds);
    }
}
