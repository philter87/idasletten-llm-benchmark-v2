using Idasletten.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [BindProperty(SupportsGet = true)]
    public Guid UserId { get; set; }

    public UserStatsDto? UserStats { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        UserStats = await _mediator.Send(new GetUserStatsQuery(UserId), cancellationToken);
        if (UserStats == null) return NotFound();
        return Page();
    }
}
