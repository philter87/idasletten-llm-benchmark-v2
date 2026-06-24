using Idasletten.Features.Matches.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchDetailModel : PageModel
{
    private readonly IMediator _mediator;

    public MatchViewModel? Match { get; set; }

    public MatchDetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid tournamentId, Guid matchId)
    {
        Match = await _mediator.Send(new GetMatchByIdQuery(tournamentId, matchId));
    }
}
