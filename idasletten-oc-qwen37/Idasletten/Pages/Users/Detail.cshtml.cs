using Idasletten.Features.Players.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public PlayerStatsDto? Stats { get; set; }

    public async Task OnGetAsync(Guid userId)
    {
        Stats = await _mediator.Send(new GetPlayerStatsQuery(userId));
    }
}
