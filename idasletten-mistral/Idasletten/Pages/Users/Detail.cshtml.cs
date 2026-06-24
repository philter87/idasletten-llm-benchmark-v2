using Idasletten.Shared.Data;
using Idasletten.Shared.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Users;

public class DetailModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public DetailModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public User User { get; set; } = default!;
    public List<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
    public List<TournamentMatch> RecentMatches { get; set; } = new List<TournamentMatch>();
    
    public class UserStatistics
    {
        public int TotalTournaments { get; set; }
        public int TotalMatches { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int TotalPointsWon { get; set; }
    }
    
    public UserStatistics Statistics { get; set; } = new UserStatistics();
    
    public async Task<IActionResult> OnGetAsync(string id)
    {
        User = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id || u.UserName == id);
        
        if (User == null)
        {
            return NotFound();
        }
        
        // Get all tournament players for this user
        TournamentPlayers = await _context.TournamentPlayers
            .Where(tp => tp.UserId == User.Id)
            .Include(tp => tp.Tournament)
            .Include(tp => tp.User)
            .OrderByDescending(tp => tp.Score)
            .ToListAsync();
        
        // Get recent matches
        var tournamentIds = TournamentPlayers.Select(tp => tp.TournamentId).ToList();
        RecentMatches = await _context.TournamentMatches
            .Where(m => tournamentIds.Contains(m.TournamentId))
            .Include(m => m.Tournament)
            .Include(m => m.Teams)
                .ThenInclude(t => t.Players)
            .Include(m => m.Results)
                .ThenInclude(r => r.Team)
            .OrderByDescending(m => m.CompletedAt ?? m.CreatedAt)
            .ToListAsync();
        
        // Calculate statistics
        Statistics.TotalTournaments = TournamentPlayers.Count;
        Statistics.TotalMatches = TournamentPlayers.Sum(tp => tp.MatchCount);
        Statistics.TotalWins = TournamentPlayers.Sum(tp => tp.WinCount);
        Statistics.TotalLosses = TournamentPlayers.Sum(tp => tp.LoseCount);
        Statistics.TotalPointsWon = TournamentPlayers.Sum(tp => tp.PointsWon);
        
        return Page();
    }
}
