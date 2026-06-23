using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<TournamentCard> Tournaments { get; private set; } = [];
    public async Task OnGetAsync() => Tournaments = await mediator.Send(new ListTournamentsQuery());
}
