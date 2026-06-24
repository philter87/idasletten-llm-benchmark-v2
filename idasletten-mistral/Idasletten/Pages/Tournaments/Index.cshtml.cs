using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public List<Tournament> Tournaments { get; set; } = new List<Tournament>();
    
    public async Task OnGetAsync()
    {
        Tournaments = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
            .Include(t => t.Matches)
            .Include(t => t.ParentTournament)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}
