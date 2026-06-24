using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;

    public PlayersModel(IMediator mediator, AppDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    public Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
    public List<TournamentPlayer> Players { get; set; } = [];
    public List<Tournament> AllTournaments { get; set; } = [];
    public Tournament? SeedTournament { get; set; }
    public List<TournamentPlayer> SeedTournamentPlayers { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        TournamentId = tournamentId;
        Tournament = await _mediator.Send(new GetTournamentByIdQuery(tournamentId));
        if (Tournament == null) return NotFound();

        Players = Tournament.Players.ToList();
        AllTournaments = (await _mediator.Send(new GetTournamentsQuery(IncludeArchived: true)))
            .Where(t => t.Id != tournamentId).ToList();

        if (Tournament.SeedTournamentId.HasValue)
        {
            SeedTournament = await _mediator.Send(new GetTournamentByIdQuery(Tournament.SeedTournamentId.Value));
            SeedTournamentPlayers = SeedTournament?.Players
                .OrderByDescending(p => p.Score)
                .ToList() ?? [];
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string username, string? name)
    {
        await _mediator.Send(new AddPlayerToTournamentCommand(username, name, tournamentId));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostAddFromSeedAsync(Guid tournamentId, string username, string? name)
    {
        await _mediator.Send(new AddPlayerToTournamentCommand(username, name, tournamentId));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostSetSeedAsync(Guid tournamentId, Guid seedTournamentId)
    {
        var tournament = await _db.Tournaments.FindAsync(tournamentId);
        if (tournament != null)
        {
            tournament.SeedTournamentId = seedTournamentId;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { tournamentId });
    }
}
