using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

public class GetTournamentPlayersQueryHandler : IRequestHandler<GetTournamentPlayersQuery, List<TournamentPlayer>>
{
    private readonly ApplicationDbContext _context;
    
    public GetTournamentPlayersQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<TournamentPlayer>> Handle(GetTournamentPlayersQuery request, CancellationToken cancellationToken)
    {
        return await _context.TournamentPlayers
            .Where(tp => tp.TournamentId == request.TournamentId)
            .Include(tp => tp.User)
            .OrderByDescending(tp => tp.Score)
            .ThenByDescending(tp => tp.ScoreDiff)
            .ToListAsync(cancellationToken);
    }
}
