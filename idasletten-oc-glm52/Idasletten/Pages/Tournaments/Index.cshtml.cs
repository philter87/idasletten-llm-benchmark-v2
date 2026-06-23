using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;

    public List<TournamentView> Tournaments { get; private set; } = new();

    public async Task OnGet()
        => Tournaments = await _mediator.Send(new ListTournamentsQuery(IncludeHistorical: true));
}