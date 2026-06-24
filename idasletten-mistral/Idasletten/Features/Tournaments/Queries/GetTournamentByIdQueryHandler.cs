using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public class GetTournamentByIdQueryHandler : IRequestHandler<GetTournamentByIdQuery, Tournament?>
{
    private readonly ApplicationDbContext _context;
    
    public GetTournamentByIdQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Tournament?> Handle(GetTournamentByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tournaments
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
            .Include(t => t.Teams)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
    }
}
