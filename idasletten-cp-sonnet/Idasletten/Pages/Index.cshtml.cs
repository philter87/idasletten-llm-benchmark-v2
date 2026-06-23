using Idasletten.Features.Tournaments.Queries;
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

    public IReadOnlyList<TournamentSummaryDto> PublicTournaments { get; set; } = [];

    public async Task OnGetAsync()
    {
        var all = await _mediator.Send(new GetTournamentsQuery(IncludeArchived: false, IncludePrivate: false, IncludeChildTournaments: false));
        PublicTournaments = all.Where(t => t.IsPublic && !t.IsArchived).ToList();
    }
}
