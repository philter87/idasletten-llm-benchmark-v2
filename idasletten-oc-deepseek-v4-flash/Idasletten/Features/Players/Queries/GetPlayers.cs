using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

public record GetPlayersQuery(Guid TournamentId) : IRequest<List<TournamentPlayer>>;

public class GetPlayersHandler : IRequestHandler<GetPlayersQuery, List<TournamentPlayer>>
{
    private readonly AppDbContext _db;

    public GetPlayersHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TournamentPlayer>> Handle(GetPlayersQuery request, CancellationToken cancellationToken)
    {
        return await _db.TournamentPlayers
            .Include(tp => tp.User)
            .Where(tp => tp.TournamentId == request.TournamentId)
            .OrderByDescending(tp => tp.Score)
            .ToListAsync(cancellationToken);
    }
}
