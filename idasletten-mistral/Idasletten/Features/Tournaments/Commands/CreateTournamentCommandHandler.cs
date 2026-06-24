using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

public class CreateTournamentCommandHandler : IRequestHandler<CreateTournamentCommand, Guid>
{
    private readonly ApplicationDbContext _context;
    
    public CreateTournamentCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Guid> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            TeamSize = request.TeamSize,
            PointsToWin = request.PointsToWin,
            ScoreSystem = request.ScoreSystem,
            MaxPlayerCount = request.MaxPlayerCount,
            IsPublic = request.IsPublic,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Publish event
        // Note: In a real implementation, we would publish an event here
        // For now, we'll just return the tournament ID
        
        return tournament.Id;
    }
}
