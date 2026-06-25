using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public class ListTournamentsHandler : IRequestHandler<ListTournamentsQuery, List<Tournament>>
{
    private readonly AppDbContext _db;

    public ListTournamentsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Tournament>> Handle(ListTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tournaments.AsQueryable();

        if (!request.IncludeArchived)
        {
            query = query.Where(t => !t.IsArchived);
        }

        if (!request.IncludePrivate)
        {
            query = query.Where(t => t.IsPublic && t.ParentTournamentId == null);
        }

        return await query
            .Include(t => t.Players)
            .OrderByDescending(t => t.Id)
            .ToListAsync(cancellationToken);
    }
}
