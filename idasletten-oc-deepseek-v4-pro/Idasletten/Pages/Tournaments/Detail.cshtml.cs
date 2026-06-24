using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public Shared.Entities.Tournament? Tournament { get; set; }
    public MatchesResult? Matches { get; set; }

    public DetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid tournamentId)
    {
        Tournament = await _mediator.Send(new GetTournamentByIdQuery(tournamentId));
        Matches = await _mediator.Send(new GetMatchesForTournamentQuery(tournamentId));
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string initials, string? name)
    {
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, initials, name));
        return RedirectToPage(new { tournamentId });
    }
}
