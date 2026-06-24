using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class MatchDetailModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public MatchDetailModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Tournament? Tournament { get; set; }
    public TournamentMatch? Match { get; set; }
    
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Match = await _context.TournamentMatches
            .Include(m => m.Tournament)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Players)
                    .ThenInclude(p => p.User)
            .Include(m => m.Results)
                .ThenInclude(r => r.Team)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (Match == null)
        {
            return NotFound();
        }
        
        Tournament = Match.Tournament;
        
        return Page();
    }
}
