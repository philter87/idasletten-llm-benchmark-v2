using Idasletten.Features.Users.Queries;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class ProfileModel : PageModel
{
    private readonly IMediator _mediator;

    public ProfileModel(IMediator mediator) => _mediator = mediator;

    public new User? User { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid userId)
    {
        User = await _mediator.Send(new GetUserByIdQuery(userId));
        if (User == null) return NotFound();
        return Page();
    }
}
