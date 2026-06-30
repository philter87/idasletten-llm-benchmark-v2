using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetTournaments;

public class GetTournamentsHandler(IdaslettenDbContext db)
    : IRequestHandler<GetTournamentsQuery, IReadOnlyList<TournamentSummaryDto>>
{
    public async Task<IReadOnlyList<TournamentSummaryDto>> Handle(GetTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Tournaments.AsQueryable();

        if (request.IsPublic is { } isPublic)
        {
            query = query.Where(t => t.IsPublic == isPublic);
        }

        if (!request.IncludeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        if (!request.IncludeChildren)
        {
            query = query.Where(t => t.ParentTournamentId == null);
        }

        return await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TournamentSummaryDto(
                t.Id, t.Name, t.ScoreSystem, t.IsArchived, t.IsPublic,
                db.TournamentPlayers.Count(p => p.TournamentId == t.Id),
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
