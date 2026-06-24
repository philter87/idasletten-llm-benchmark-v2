using Idasletten.Features.Matches.Queries;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchDetailModel : PageModel
{
    private readonly IMediator _mediator;

    public MatchDetailModel(IMediator mediator) => _mediator = mediator;

    public Guid TournamentId { get; set; }
    public TournamentMatch? Match { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid matchId)
    {
        TournamentId = tournamentId;
        Match = await _mediator.Send(new GetMatchByIdQuery(matchId));
        if (Match == null) return NotFound();
        return Page();
    }
}
