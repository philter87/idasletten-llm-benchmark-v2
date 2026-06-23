using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;

    public IReadOnlyList<TournamentListItem> Tournaments { get; private set; } = new List<TournamentListItem>();

    public async Task OnGet()
    {
        Tournaments = await _mediator.Send(new ListTournamentsQuery());
    }
}
