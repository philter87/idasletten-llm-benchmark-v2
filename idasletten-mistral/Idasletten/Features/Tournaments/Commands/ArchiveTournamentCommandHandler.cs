using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

public class ArchiveTournamentCommandHandler : IRequestHandler<ArchiveTournamentCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    
    public ArchiveTournamentCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Unit> Handle(ArchiveTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
        
        if (tournament == null)
        {
            throw new Exception("Tournament not found");
        }
        
        tournament.IsArchived = request.IsArchived;
        tournament.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        
        // Publish event
        // Note: In a real implementation, we would publish TournamentArchivedEvent here
        
        return Unit.Value;
    }
}
