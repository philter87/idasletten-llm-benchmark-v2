using Idasletten.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;
    public DetailModel(IMediator mediator) => _mediator = mediator;

    public UserStats Stats { get; private set; } = null!;

    public async Task<IActionResult> OnGet(Guid userId)
    {
        var stats = await _mediator.Send(new GetUserStatsQuery(userId));
        if (stats is null) return NotFound();
        Stats = stats;
        return Page();
    }
}
