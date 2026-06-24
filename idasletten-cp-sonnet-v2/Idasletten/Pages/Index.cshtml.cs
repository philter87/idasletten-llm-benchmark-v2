using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
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

    public List<Tournament> PublicTournaments { get; set; } = [];

    public async Task OnGetAsync()
    {
        PublicTournaments = await _mediator.Send(new GetPublicTournamentsQuery());
    }
}
