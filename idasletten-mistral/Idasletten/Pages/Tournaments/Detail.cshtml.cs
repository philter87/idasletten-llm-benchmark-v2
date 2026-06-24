using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public DetailModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Tournament Tournament { get; set; } = default!;
    public List<TournamentMatch> NextMatches { get; set; } = new List<TournamentMatch>();
    public List<TournamentMatch> RecentMatches { get; set; } = new List<TournamentMatch>();
    
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Tournament = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
                .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Teams)
                    .ThenInclude(tt => tt.Players)
                        .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Results)
                    .ThenInclude(r => r.Team)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (Tournament == null)
        {
            return NotFound();
        }
        
        // Get next 5 planned matches
        NextMatches = await _context.TournamentMatches
            .Where(m => m.TournamentId == id && m.State == MatchState.Planned)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Players)
                    .ThenInclude(p => p.User)
            .OrderBy(m => m.Order)
            .Take(5)
            .ToListAsync();
        
        // Get recent 5 played matches
        RecentMatches = await _context.TournamentMatches
            .Where(m => m.TournamentId == id && m.State == MatchState.Done)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Players)
                    .ThenInclude(p => p.User)
            .Include(m => m.Results)
                .ThenInclude(r => r.Team)
            .OrderByDescending(m => m.CompletedAt)
            .Take(5)
            .ToListAsync();
        
        return Page();
    }
}
