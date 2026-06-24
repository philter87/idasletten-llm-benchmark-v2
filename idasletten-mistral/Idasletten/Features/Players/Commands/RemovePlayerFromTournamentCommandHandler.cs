using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public class RemovePlayerFromTournamentCommandHandler : IRequestHandler<RemovePlayerFromTournamentCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    
    public RemovePlayerFromTournamentCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Unit> Handle(RemovePlayerFromTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournamentPlayer = await _context.TournamentPlayers
            .Include(tp => tp.Tournament)
            .FirstOrDefaultAsync(tp => tp.TournamentId == request.TournamentId && tp.Id == request.PlayerId, cancellationToken);
        
        if (tournamentPlayer == null)
        {
            throw new Exception("Player not found in tournament");
        }
        
        // Remove the player from all teams in this tournament
        var teams = await _context.TournamentTeams
            .Where(t => t.TournamentId == request.TournamentId && t.Players.Any(p => p.Id == tournamentPlayer.Id))
            .Include(t => t.Players)
            .ToListAsync(cancellationToken);
        
        foreach (var team in teams)
        {
            team.Players.Remove(tournamentPlayer);
        }
        
        // Remove the tournament player
        _context.TournamentPlayers.Remove(tournamentPlayer);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Publish event
        // Note: In a real implementation, we would publish PlayerRemovedFromTournamentEvent here
        
        return Unit.Value;
    }
}
