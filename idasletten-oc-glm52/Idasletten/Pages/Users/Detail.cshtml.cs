using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Users;

public class DetailModel : PageModel
{
    private readonly IdaslettenDbContext _db;
    public DetailModel(IdaslettenDbContext db) => _db = db;

    public UserViewVm UserView { get; private set; } = null!;

    public async Task<IActionResult> OnGet(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        var players = await _db.TournamentPlayers.AsNoTracking()
            .Where(p => p.UserId == id)
            .Join(_db.Tournaments.AsNoTracking(), p => p.TournamentId, t => t.Id,
                (p, t) => new UserTournamentStat { TournamentId = t.Id, TournamentName = t.Name, ScoreSystem = t.ScoreSystem, Score = p.Score, WinCount = p.WinCount, LoseCount = p.LoseCount, MatchCount = p.MatchCount, PointsWon = p.PointsWon, PointsLost = p.PointsLost })
            .ToListAsync();
        UserView = new UserViewVm { Id = user.Id, Username = user.Username, Name = user.Name, Email = user.Email, ImageUrl = user.ImageUrl, Tournaments = players };
        return Page();
    }

    public class UserViewVm
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Email { get; set; }
        public string? ImageUrl { get; set; }
        public List<UserTournamentStat> Tournaments { get; set; } = new();
    }

    public class UserTournamentStat
    {
        public Guid TournamentId { get; set; }
        public string TournamentName { get; set; } = "";
        public ScoreSystem ScoreSystem { get; set; }
        public double Score { get; set; }
        public int WinCount { get; set; }
        public int LoseCount { get; set; }
        public int MatchCount { get; set; }
        public int PointsWon { get; set; }
        public int PointsLost { get; set; }
    }
}