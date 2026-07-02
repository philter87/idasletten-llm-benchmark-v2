using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

/// <summary>
/// Lists tournaments. Child tournaments (later rounds) are hidden by default.
/// </summary>
public record GetTournamentsQuery(
    bool PublicOnly = false,
    bool IncludeArchived = true,
    bool IncludeChildren = false) : IRequest<List<TournamentListItem>>;

public record TournamentListItem(Tournament Tournament, int PlayerCount, int MatchCount);

public class GetTournamentsHandler(AppDbContext db) : IRequestHandler<GetTournamentsQuery, List<TournamentListItem>>
{
    public async Task<List<TournamentListItem>> Handle(GetTournamentsQuery request, CancellationToken ct)
    {
        var query = db.Tournaments.AsNoTracking().AsQueryable();

        if (request.PublicOnly)
            query = query.Where(t => t.IsPublic && !t.IsArchived);
        if (!request.IncludeArchived)
            query = query.Where(t => !t.IsArchived);
        if (!request.IncludeChildren)
            query = query.Where(t => t.ParentTournamentId == null);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TournamentListItem(
                t,
                db.TournamentPlayers.Count(p => p.TournamentId == t.Id),
                db.TournamentMatches.Count(m => m.TournamentId == t.Id)))
            .ToListAsync(ct);
    }
}
