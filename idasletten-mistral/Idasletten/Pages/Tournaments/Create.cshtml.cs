using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Idasletten.Shared.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [BindProperty]
    public Tournament Tournament { get; set; } = new Tournament
    {
        TeamSize = 2,
        PointsToWin = 5,
        ScoreSystem = ScoreSystem.TrueSkill,
        IsPublic = true
    };
    
    public async Task<IActionResult> OnGetAsync()
    {
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync(string action)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        // Set defaults
        Tournament.Id = Guid.NewGuid();
        Tournament.CreatedAt = DateTime.UtcNow;
        
        // Add to database
        _context.Tournaments.Add(Tournament);
        await _context.SaveChangesAsync();
        
        // Navigate based on action
        if (action == "createAndPlan")
        {
            // Navigate to players page to add players first, then plan matches
            return RedirectToPage("/Tournaments/Players", new { id = Tournament.Id });
        }
        
        return RedirectToPage("/Tournaments/Detail", new { id = Tournament.Id });
    }
}
