using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class MatchesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public MatchesModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Tournament Tournament { get; set; } = default!;
    public List<TournamentMatch> PlannedMatches { get; set; } = new List<TournamentMatch>();
    public List<TournamentMatch> CompletedMatches { get; set; } = new List<TournamentMatch>();
    public List<TournamentMatch> CancelledMatches { get; set; } = new List<TournamentMatch>();
    public List<Tournament> PreviousTournaments { get; set; } = new List<Tournament>();
    
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (Tournament == null)
        {
            return NotFound();
        }
        
        // Get all matches with team and player data
        var allMatches = await _context.TournamentMatches
            .Where(m => m.TournamentId == id)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Players)
                    .ThenInclude(p => p.User)
            .Include(m => m.Results)
                .ThenInclude(r => r.Team)
            .OrderBy(m => m.Order)
            .ToListAsync();
        
        PlannedMatches = allMatches.Where(m => m.State == MatchState.Planned).ToList();
        CompletedMatches = allMatches.Where(m => m.State == MatchState.Done).ToList();
        CancelledMatches = allMatches.Where(m => m.State == MatchState.Cancelled).ToList();
        
        // Get previous tournaments for seeding
        PreviousTournaments = await _context.Tournaments
            .Where(t => t.Id != id && !t.IsArchived)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();
        
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync(Guid tournamentId, string? matchId, string? seedTournamentId, 
        int gamesPerPlayer = 3, string seedingType = "Random", bool fixedTeams = false)
    {
        // Handle cancel match
        if (!string.IsNullOrEmpty(matchId) && Guid.TryParse(matchId, out var matchIdGuid))
        {
            var matchToCancel = await _context.TournamentMatches
                .FirstOrDefaultAsync(m => m.Id == matchIdGuid && m.TournamentId == tournamentId);
            
            if (matchToCancel != null && matchToCancel.State == MatchState.Planned)
            {
                matchToCancel.State = MatchState.Cancelled;
                await _context.SaveChangesAsync();
            }
            
            return RedirectToPage(new { id = tournamentId });
        }
        
        // Handle plan several matches
        if (!string.IsNullOrEmpty(seedTournamentId))
        {
            // This would implement the match planning algorithm
            // For now, just redirect back
        }
        
        return RedirectToPage(new { id = tournamentId });
    }
    
    public string GetTeamColor(int teamNumber)
    {
        var colors = new[] { "#e74c3c", "#3498db", "#2ecc71", "#f39c12", "#9b59b6", "#1abc9c" };
        return colors[teamNumber % colors.Length];
    }
}
