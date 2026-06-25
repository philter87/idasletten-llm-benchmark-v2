using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public class ListMatchesHandler : IRequestHandler<ListMatchesQuery, List<TournamentMatch>>
{
    private readonly AppDbContext _db;

    public ListMatchesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TournamentMatch>> Handle(ListMatchesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .AsQueryable();

        if (request.State.HasValue)
        {
            query = query.Where(m => m.State == request.State.Value);
        }

        return await query
            .Include(m => m.TeamResults)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.Players)
                        .ThenInclude(p => p.User)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);
    }
}
