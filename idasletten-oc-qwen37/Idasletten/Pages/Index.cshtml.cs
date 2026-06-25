using Idasletten.Features.Tournaments.Queries;
using Idasletten.Models;
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

    public List<Tournament> PublicTournaments { get; set; } = new();

    public async Task OnGetAsync()
    {
        PublicTournaments = await _mediator.Send(new ListTournamentsQuery(IncludeArchived: false, IncludePrivate: false));
    }
}
