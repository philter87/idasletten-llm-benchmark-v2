using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public List<Tournament> PublicTournaments { get; set; } = new List<Tournament>();
    
    public async Task OnGetAsync()
    {
        PublicTournaments = await _context.Tournaments
            .Where(t => t.IsPublic && !t.IsArchived)
            .Include(t => t.TournamentPlayers)
            .Include(t => t.Matches)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}
