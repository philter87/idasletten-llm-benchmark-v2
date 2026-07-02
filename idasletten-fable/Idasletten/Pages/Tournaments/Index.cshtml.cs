using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel(IMediator mediator) : PageModel
{
    public List<TournamentListItem> Tournaments { get; private set; } = [];

    public async Task OnGet()
    {
        Tournaments = await mediator.Send(new GetTournamentsQuery(IncludeArchived: true, IncludeChildren: false));
    }
}
