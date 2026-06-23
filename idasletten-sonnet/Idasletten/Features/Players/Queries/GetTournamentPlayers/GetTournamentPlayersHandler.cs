using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries.GetTournamentPlayers;

public class GetTournamentPlayersHandler(AppDbContext db) : IRequestHandler<GetTournamentPlayersQuery, List<TournamentPlayer>>
{
    public Task<List<TournamentPlayer>> Handle(GetTournamentPlayersQuery request, CancellationToken cancellationToken)
        => db.TournamentPlayers
             .Include(tp => tp.User)
             .Where(tp => tp.TournamentId == request.TournamentId)
             .OrderByDescending(tp => tp.Score)
             .ToListAsync(cancellationToken);
}
