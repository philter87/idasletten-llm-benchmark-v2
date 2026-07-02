using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel(IMediator mediator) : PageModel
{
    public List<TournamentListItem> PublicTournaments { get; private set; } = [];

    public async Task OnGet()
    {
        PublicTournaments = await mediator.Send(new GetTournamentsQuery(PublicOnly: true));
    }
}
