using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

public class AddPlayerToTournamentCommandHandler : IRequestHandler<AddPlayerToTournamentCommand, TournamentPlayer>
{
    private readonly ApplicationDbContext _context;
    
    public AddPlayerToTournamentCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<TournamentPlayer> Handle(AddPlayerToTournamentCommand request, CancellationToken cancellationToken)
    {
        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
        
        if (tournament == null)
        {
            throw new Exception("Tournament not found");
        }
        
        // Check if user already exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == request.Initials, cancellationToken);
        
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.Initials,
                NormalizedUserName = request.Initials.ToUpper(),
                Email = $"{request.Initials.ToLower()}@idasletten.local",
                NormalizedEmail = $"{request.Initials.ToLower()}@IDASLETTEN.LOCAL",
                Name = request.Name ?? request.Initials
            };
            _context.Users.Add(user);
        }
        
        // Check if player already exists in this tournament
        var existingPlayer = await _context.TournamentPlayers
            .FirstOrDefaultAsync(tp => tp.UserId == user.Id && tp.TournamentId == request.TournamentId, cancellationToken);
        
        if (existingPlayer != null)
        {
            throw new Exception("Player already exists in this tournament");
        }
        
        // Calculate initial score based on scoring system
        var initialScore = tournament.ScoreSystem switch
        {
            ScoreSystem.Lives => tournament.TeamSize * 3,
            _ => 1500.0 // Default for Elo, TrueSkill, WinCount
        };
        
        var tournamentPlayer = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TournamentId = tournament.Id,
            Score = initialScore,
            WinCount = 0,
            MatchCount = 0,
            LoseCount = 0,
            Lives = tournament.ScoreSystem == ScoreSystem.Lives ? 3 : 0,
            PointsWon = 0,
            PointsLost = 0,
            ScoreDiff = 0
        };
        
        _context.TournamentPlayers.Add(tournamentPlayer);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Publish event
        // Note: In a real implementation, we would publish PlayerAddedToTournamentEvent here
        
        return tournamentPlayer;
    }
}
