using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public class GetTournamentMatchesQueryHandler : IRequestHandler<GetTournamentMatchesQuery, List<TournamentMatch>>
{
    private readonly ApplicationDbContext _context;
    
    public GetTournamentMatchesQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<TournamentMatch>> Handle(GetTournamentMatchesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Players)
                    .ThenInclude(p => p.User)
            .Include(m => m.Results)
                .ThenInclude(r => r.Team)
            .AsQueryable();
        
        if (request.State.HasValue)
        {
            query = query.Where(m => m.State == request.State);
        }
        
        return await query.OrderBy(m => m.Order).ToListAsync(cancellationToken);
    }
}
