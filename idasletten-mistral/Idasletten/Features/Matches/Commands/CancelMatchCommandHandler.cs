using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Commands;

public class CancelMatchCommandHandler : IRequestHandler<CancelMatchCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    
    public CancelMatchCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Unit> Handle(CancelMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await _context.TournamentMatches
            .Include(m => m.Teams)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);
        
        if (match == null)
        {
            throw new Exception("Match not found");
        }
        
        // Only cancel if the match is still in Planned state
        if (match.State != MatchState.Planned)
        {
            throw new Exception("Only planned matches can be cancelled");
        }
        
        match.State = MatchState.Cancelled;
        match.UpdatedAt = DateTime.UtcNow;
        
        // Remove any results associated with this match
        var results = await _context.TournamentTeamMatchResults
            .Where(r => r.MatchId == match.Id)
            .ToListAsync(cancellationToken);
        
        _context.TournamentTeamMatchResults.RemoveRange(results);
        
        await _context.SaveChangesAsync(cancellationToken);
        
        // Publish event
        // Note: In a real implementation, we would publish MatchCancelledEvent here
        
        return Unit.Value;
    }
}
