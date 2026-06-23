using Idasletten.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailsModel(IMediator mediator) : PageModel
{
    public UserDetail UserDetail { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(Guid userId)
    {
        var detail = await mediator.Send(new GetUserDetailQuery(userId));
        if (detail is null) return NotFound();
        UserDetail = detail;
        return Page();
    }
}
