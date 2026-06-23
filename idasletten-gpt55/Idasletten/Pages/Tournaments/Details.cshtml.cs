using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailsModel(IMediator mediator) : PageModel
{
    public TournamentDetail Tournament { get; private set; } = null!;
    [BindProperty] public string Initials { get; set; } = "";
    [BindProperty] public string? Name { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        var detail = await mediator.Send(new GetTournamentDetailQuery(tournamentId));
        if (detail is null) return NotFound();
        Tournament = detail;
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId)
    {
        await mediator.Send(new AddPlayerToTournamentCommand(tournamentId, Initials, Name));
        return RedirectToPage(new { tournamentId });
    }
}
