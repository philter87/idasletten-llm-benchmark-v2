using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record GetMatchesQuery(Guid TournamentId) : IRequest<List<TournamentMatch>>;

public class GetMatchesHandler : IRequestHandler<GetMatchesQuery, List<TournamentMatch>>
{
    private readonly AppDbContext _db;

    public GetMatchesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TournamentMatch>> Handle(GetMatchesQuery request, CancellationToken cancellationToken)
    {
        return await _db.TournamentMatches
            .Include(m => m.TeamEntries)
                .ThenInclude(te => te.Team)
                    .ThenInclude(t => t.PlayerEntries)
                        .ThenInclude(tp => tp.Player)
                            .ThenInclude(p => p.User)
            .Include(m => m.Results)
            .Where(m => m.TournamentId == request.TournamentId)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);
    }
}
