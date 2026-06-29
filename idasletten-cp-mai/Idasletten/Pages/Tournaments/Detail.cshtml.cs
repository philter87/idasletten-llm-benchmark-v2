using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    public TournamentDetailDto? Tournament { get; set; }
    public List<TournamentPlayerDto> Players { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Tournament = await _mediator.Send(new GetTournamentDetailQuery(TournamentId), cancellationToken);
        if (Tournament == null) return NotFound();
        Players = await _mediator.Send(new GetTournamentPlayersQuery(TournamentId), cancellationToken);
        return Page();
    }
}
