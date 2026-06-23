using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<TournamentListItem> PublicTournaments { get; private set; } = new List<TournamentListItem>();

    public async Task OnGet()
    {
        PublicTournaments = await _mediator.Send(new ListPublicTournamentsQuery());
    }
}
