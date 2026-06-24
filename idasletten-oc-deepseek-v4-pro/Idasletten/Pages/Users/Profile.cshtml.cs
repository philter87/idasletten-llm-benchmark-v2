using Idasletten.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class ProfileModel : PageModel
{
    private readonly IMediator _mediator;

    public Shared.Entities.User? UserProfile { get; set; }

    public ProfileModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid userId)
    {
        UserProfile = await _mediator.Send(new GetUserByIdQuery(userId));
    }
}
