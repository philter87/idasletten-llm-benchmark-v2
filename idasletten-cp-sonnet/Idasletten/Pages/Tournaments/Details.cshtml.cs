using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailsModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public TournamentDto? Tournament { get; set; }

    public async Task OnGetAsync(Guid id)
    {
        Tournament = await _mediator.Send(new GetTournamentQuery(id));
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string username, string? name)
    {
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, username.ToUpperInvariant(), name));
        return RedirectToPage(new { id = tournamentId });
    }
}
