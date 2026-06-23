using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetAllTournaments;

public class GetAllTournamentsHandler(AppDbContext db) : IRequestHandler<GetAllTournamentsQuery, List<Tournament>>
{
    public Task<List<Tournament>> Handle(GetAllTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Tournaments
            .Include(t => t.Players)
            .AsQueryable();

        if (!request.IncludeChildren)
            query = query.Where(t => t.ParentTournamentId == null);

        if (!request.IncludeArchived)
            query = query.Where(t => !t.IsArchived);

        return query.OrderByDescending(t => t.CreatedAt).ToListAsync(cancellationToken);
    }
}
