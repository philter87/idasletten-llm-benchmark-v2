using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public List<Shared.Entities.Tournament> Tournaments { get; set; } = [];

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        Tournaments = await _mediator.Send(new GetTournamentsQuery(IncludeArchived: true));
    }
}
