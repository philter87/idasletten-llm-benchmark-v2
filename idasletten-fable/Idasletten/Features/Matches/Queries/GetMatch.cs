using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record GetMatchQuery(Guid TournamentId, Guid MatchId) : IRequest<TournamentMatch?>;

public class GetMatchHandler(AppDbContext db) : IRequestHandler<GetMatchQuery, TournamentMatch?>
{
    public async Task<TournamentMatch?> Handle(GetMatchQuery request, CancellationToken ct)
    {
        return await db.TournamentMatches.AsNoTracking()
            .Include(m => m.Results)
            .ThenInclude(r => r.Team)
            .ThenInclude(t => t.Players)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId && m.TournamentId == request.TournamentId, ct);
    }
}
