using Idasletten.Features.Matches.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchDetailModel : PageModel
{
    private readonly IMediator _mediator;

    public MatchDetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public MatchDetailDto? Match { get; set; }
    public Guid TournamentId { get; set; }

    public async Task OnGetAsync(Guid id, Guid matchId)
    {
        TournamentId = id;
        Match = await _mediator.Send(new GetMatchQuery(matchId));
    }
}
