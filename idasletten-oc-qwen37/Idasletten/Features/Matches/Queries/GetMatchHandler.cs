using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public class GetMatchHandler : IRequestHandler<GetMatchQuery, TournamentMatch?>
{
    private readonly AppDbContext _db;

    public GetMatchHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TournamentMatch?> Handle(GetMatchQuery request, CancellationToken cancellationToken)
    {
        return await _db.TournamentMatches
            .Include(m => m.Tournament)
            .Include(m => m.TeamResults)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.Players)
                        .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);
    }
}
