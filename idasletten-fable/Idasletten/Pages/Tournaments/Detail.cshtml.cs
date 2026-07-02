using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel(IMediator mediator) : PageModel
{
    public TournamentDetailResult Detail { get; private set; } = null!;

    public async Task<IActionResult> OnGet(Guid tournamentId)
    {
        var detail = await mediator.Send(new GetTournamentDetailQuery(tournamentId));
        if (detail is null)
            return NotFound();
        Detail = detail;
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayer(Guid tournamentId, string initials, string? name)
    {
        try
        {
            await mediator.Send(new AddPlayerToTournamentCommand(tournamentId, initials, name));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect($"/tournaments/{tournamentId}");
    }

    public async Task<IActionResult> OnPostCreateNextRound(Guid tournamentId, int? topPlayerCount)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect($"/login?returnUrl=/tournaments/{tournamentId}");

        var child = await mediator.Send(new CreateNextRoundCommand(tournamentId, topPlayerCount));
        return Redirect($"/tournaments/{child.Id}");
    }
}
