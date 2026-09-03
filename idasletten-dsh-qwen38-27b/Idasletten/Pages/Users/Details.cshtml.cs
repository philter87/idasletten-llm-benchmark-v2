using Idasletten.Features.Users.Queries.GetUserStats;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailsModel(IMediator mediator) => _mediator = mediator;

    public UserStatsDto? User { get; set; }

    public async Task OnGetAsync(Guid id)
    {
        User = await _mediator.Send(new GetUserStatsQuery(id));
        if (User is null) NotFound();
        return;
    }
}
