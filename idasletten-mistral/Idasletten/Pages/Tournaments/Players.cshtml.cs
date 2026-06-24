using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class PlayersModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public PlayersModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Tournament Tournament { get; set; } = default!;
    public List<Tournament> PreviousTournaments { get; set; } = new List<Tournament>();
    
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Tournament = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (Tournament == null)
        {
            return NotFound();
        }
        
        // Get previous tournaments for seeding
        PreviousTournaments = await _context.Tournaments
            .Where(t => t.Id != id && !t.IsArchived)
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();
        
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync(Guid tournamentId, string? playerId, string? initials, string? name)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        
        if (tournament == null)
        {
            return NotFound();
        }
        
        // Handle remove player
        if (!string.IsNullOrEmpty(playerId) && Guid.TryParse(playerId, out var playerIdGuid))
        {
            var playerToRemove = await _context.TournamentPlayers
                .FirstOrDefaultAsync(tp => tp.Id == playerIdGuid && tp.TournamentId == tournamentId);
            
            if (playerToRemove != null)
            {
                _context.TournamentPlayers.Remove(playerToRemove);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToPage(new { id = tournamentId });
        }
        
        // Handle add player
        if (!string.IsNullOrEmpty(initials))
        {
            // Check if user already exists with these initials
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == initials);
            
            if (user == null)
            {
                // Create new user
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = initials,
                    NormalizedUserName = initials.ToUpper(),
                    Email = $"{initials.ToLower()}@idasletten.local",
                    NormalizedEmail = $"{initials.ToLower()}@IDASLETTEN.LOCAL",
                    EmailConfirmed = false,
                    Name = name ?? initials
                };
                _context.Users.Add(user);
            }
            
            // Check if player already exists in this tournament
            var existingPlayer = await _context.TournamentPlayers
                .FirstOrDefaultAsync(tp => tp.UserId == user.Id && tp.TournamentId == tournamentId);
            
            if (existingPlayer == null)
            {
                // Add player to tournament
                var tournamentPlayer = new TournamentPlayer
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TournamentId = tournamentId,
                    Score = tournament.ScoreSystem == Shared.Data.Enums.ScoreSystem.Lives ? tournament.TeamSize * 3 : 1500.0,
                    WinCount = 0,
                    MatchCount = 0,
                    LoseCount = 0,
                    Lives = tournament.ScoreSystem == Shared.Data.Enums.ScoreSystem.Lives ? 3 : 0,
                    PointsWon = 0,
                    PointsLost = 0,
                    ScoreDiff = 0
                };
                
                _context.TournamentPlayers.Add(tournamentPlayer);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToPage(new { id = tournamentId });
        }
        
        return RedirectToPage(new { id = tournamentId });
    }
}
