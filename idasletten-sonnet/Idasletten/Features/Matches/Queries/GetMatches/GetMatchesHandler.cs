using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries.GetMatches;

public class GetMatchesHandler(AppDbContext db) : IRequestHandler<GetMatchesQuery, List<TournamentMatch>>
{
    public Task<List<TournamentMatch>> Handle(GetMatchesQuery request, CancellationToken cancellationToken)
    {
        var query = db.TournamentMatches
            .Include(m => m.TeamResults).ThenInclude(r => r.Team).ThenInclude(t => t.Players).ThenInclude(p => p.User)
            .Where(m => m.TournamentId == request.TournamentId);

        if (request.State.HasValue)
            query = query.Where(m => m.State == request.State.Value);

        return query.OrderBy(m => m.Order).ToListAsync(cancellationToken);
    }
}
