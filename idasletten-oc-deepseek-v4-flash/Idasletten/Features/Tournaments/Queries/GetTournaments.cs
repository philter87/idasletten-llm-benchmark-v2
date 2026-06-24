using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record GetTournamentsQuery(bool IncludeArchived = false, bool IncludeChildren = false) : IRequest<List<Tournament>>;

public class GetTournamentsHandler : IRequestHandler<GetTournamentsQuery, List<Tournament>>
{
    private readonly AppDbContext _db;

    public GetTournamentsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Tournament>> Handle(GetTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tournaments.AsQueryable();

        if (!request.IncludeArchived)
            query = query.Where(t => !t.IsArchived);

        if (!request.IncludeChildren)
            query = query.Where(t => t.ParentTournamentId == null);

        return await query.OrderByDescending(t => t.Name).ToListAsync(cancellationToken);
    }
}
