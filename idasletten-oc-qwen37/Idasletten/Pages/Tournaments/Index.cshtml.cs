using Idasletten.Features.Tournaments.Queries;
using Idasletten.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public List<Tournament> Tournaments { get; set; } = new();

    public async Task OnGetAsync(bool all = false)
    {
        Tournaments = await _mediator.Send(new ListTournamentsQuery(IncludeArchived: all, IncludePrivate: all));
    }
}
