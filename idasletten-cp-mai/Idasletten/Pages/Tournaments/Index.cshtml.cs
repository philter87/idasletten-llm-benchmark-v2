using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public List<TournamentListItemDto> Tournaments { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Tournaments = await _mediator.Send(new ListTournamentsQuery(IncludeHistorical: true), cancellationToken);
    }
}
