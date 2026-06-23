using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel(IMediator mediator, IdaslettenDbContext db) : PageModel
{
    public TournamentDetail Tournament { get; private set; } = null!;
    public IReadOnlyList<TournamentCard> SeedTournaments { get; private set; } = [];
    [BindProperty] public string Initials { get; set; } = "";
    [BindProperty] public string? Name { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        var tournament = await mediator.Send(new GetTournamentDetailQuery(tournamentId));
        if (tournament is null) return NotFound();
        Tournament = tournament;
        SeedTournaments = await mediator.Send(new ListTournamentsQuery(Historical: true));
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId)
    {
        await mediator.Send(new AddPlayerToTournamentCommand(tournamentId, Initials, Name));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostAddFromSeedAsync(Guid tournamentId, Guid userId)
    {
        var user = await db.Users.SingleAsync(u => u.Id == userId);
        await mediator.Send(new AddPlayerToTournamentCommand(tournamentId, user.UserName, user.Name));
        return RedirectToPage(new { tournamentId });
    }
}
