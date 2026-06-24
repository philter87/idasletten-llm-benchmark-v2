using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record GetMatchByIdQuery(Guid Id) : IRequest<TournamentMatch?>;

public class GetMatchByIdHandler : IRequestHandler<GetMatchByIdQuery, TournamentMatch?>
{
    private readonly AppDbContext _db;

    public GetMatchByIdHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TournamentMatch?> Handle(GetMatchByIdQuery request, CancellationToken cancellationToken)
    {
        return await _db.TournamentMatches
            .Include(m => m.TeamEntries)
                .ThenInclude(te => te.Team)
                    .ThenInclude(t => t.PlayerEntries)
                        .ThenInclude(tp => tp.Player)
                            .ThenInclude(p => p.User)
            .Include(m => m.Results)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
    }
}
