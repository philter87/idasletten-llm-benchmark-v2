using Idasletten.Features.Players.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IMediator _mediator;

    public PlayersModel(ApplicationDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    public Tournament? Tournament { get; set; }
    public List<TournamentPlayerDto> Players { get; set; } = [];
    public List<SeedPlayerOption> SeedOptions { get; set; } = [];

    public class SeedPlayerOption
    {
        public Guid UserId { get; set; }
        public string Initials { get; set; } = string.Empty;
        public bool IsAdded { get; set; }
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Tournament = await _db.Tournaments.FindAsync(new object[] { TournamentId }, cancellationToken);
        Players = await _mediator.Send(new GetTournamentPlayersQuery(TournamentId), cancellationToken);

        if (Tournament?.SeedTournamentId.HasValue == true)
        {
            var seedPlayers = await _db.TournamentPlayers
                .AsNoTracking()
                .Where(p => p.TournamentId == Tournament.SeedTournamentId.Value)
                .Include(p => p.User)
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.PointsWon - p.PointsLost)
                .ToListAsync(cancellationToken);

            var existingUserIds = Players.Select(p => p.UserId).ToHashSet();
            SeedOptions = seedPlayers.Select(p => new SeedPlayerOption
            {
                UserId = p.UserId,
                Initials = p.User.Username,
                IsAdded = existingUserIds.Contains(p.UserId)
            }).ToList();
        }
        else
        {
            SeedOptions = await _db.Tournaments
                .AsNoTracking()
                .Where(t => t.Id != TournamentId && t.ParentTournamentId == null)
                .OrderByDescending(t => t.RoundNumber)
                .ThenBy(t => t.Name)
                .Select(t => new SeedPlayerOption { UserId = t.Id, Initials = t.Name })
                .ToListAsync(cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(string initials, string? name, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddPlayerToTournamentCommand(TournamentId, initials, name), cancellationToken);
        return RedirectToPage(new { tournamentId = TournamentId });
    }

    public async Task<IActionResult> OnPostAddSeedPlayerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments.FindAsync(new object[] { TournamentId }, cancellationToken);
        if (tournament?.SeedTournamentId == null) return RedirectToPage(new { tournamentId = TournamentId });

        var player = await _db.TournamentPlayers
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.TournamentId == tournament.SeedTournamentId.Value && p.UserId == userId, cancellationToken);
        if (player == null) return RedirectToPage(new { tournamentId = TournamentId });

        await _mediator.Send(new AddPlayerToTournamentCommand(TournamentId, player.User.Username, player.User.Name), cancellationToken);
        return RedirectToPage(new { tournamentId = TournamentId });
    }

    public async Task<IActionResult> OnPostSetSeedTournamentAsync(Guid seedTournamentId, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments.FindAsync(new object[] { TournamentId }, cancellationToken);
        if (tournament != null && tournament.ParentTournamentId == null)
        {
            tournament.SeedTournamentId = seedTournamentId;
            await _db.SaveChangesAsync(cancellationToken);
        }
        return RedirectToPage(new { tournamentId = TournamentId });
    }
}
