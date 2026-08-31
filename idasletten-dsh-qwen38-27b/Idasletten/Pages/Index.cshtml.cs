using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Queries.GetPublicTournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public List<TournamentCardDto> Tournaments { get; set; } = new();

    public async Task OnGetAsync()
    {
        Tournaments = (await _mediator.Send(new GetPublicTournamentsQuery())).ToList();
    }
}
