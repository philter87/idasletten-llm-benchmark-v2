using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public List<Tournament> Tournaments { get; set; } = [];

    public async Task OnGetAsync()
    {
        Tournaments = await _mediator.Send(new GetTournamentsQuery(IncludeArchived: true));
    }
}
