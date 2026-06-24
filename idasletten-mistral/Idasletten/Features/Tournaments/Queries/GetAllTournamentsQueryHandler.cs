using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public class GetAllTournamentsQueryHandler : IRequestHandler<GetAllTournamentsQuery, List<Tournament>>
{
    private readonly ApplicationDbContext _context;
    
    public GetAllTournamentsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Tournament>> Handle(GetAllTournamentsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tournaments
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
            .Include(t => t.ParentTournament)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
