using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Queries.GetAllTournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator) => _mediator = mediator;

    public List<TournamentCardDto> Tournaments { get; set; } = new();
    public bool IncludeChildren { get; set; }

    public async Task OnGetAsync(bool? includeChildren)
    {
        IncludeChildren = includeChildren == true;
        Tournaments = (await _mediator.Send(new GetAllTournamentsQuery(IncludeChildren))).ToList();
    }
}
