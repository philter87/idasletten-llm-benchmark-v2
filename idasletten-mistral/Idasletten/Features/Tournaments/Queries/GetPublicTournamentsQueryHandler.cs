using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public class GetPublicTournamentsQueryHandler : IRequestHandler<GetPublicTournamentsQuery, List<Tournament>>
{
    private readonly ApplicationDbContext _context;
    
    public GetPublicTournamentsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Tournament>> Handle(GetPublicTournamentsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tournaments
            .Where(t => t.IsPublic && !t.IsArchived)
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
