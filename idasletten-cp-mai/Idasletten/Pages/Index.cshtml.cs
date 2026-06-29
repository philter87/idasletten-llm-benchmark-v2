using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public List<TournamentListItemDto> PublicTournaments { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PublicTournaments = await _mediator.Send(new ListTournamentsQuery(IncludeHistorical: false), cancellationToken);
    }
}
