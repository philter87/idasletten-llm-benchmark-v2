using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;

    public List<TournamentView> PublicTournaments { get; private set; } = new();

    public async Task OnGet()
        => PublicTournaments = await _mediator.Send(new ListTournamentsQuery(IncludeHistorical: false));
}