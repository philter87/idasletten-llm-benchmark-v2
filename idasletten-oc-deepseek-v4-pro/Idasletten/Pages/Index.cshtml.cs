using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public List<Shared.Entities.Tournament> PublicTournaments { get; set; } = [];

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        PublicTournaments = await _mediator.Send(new GetPublicTournamentsQuery());
    }
}
