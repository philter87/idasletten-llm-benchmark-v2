using Idasletten.Features.Users.Queries;
using Idasletten.Shared.Entities;
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

    public User? PageUser { get; set; }

    public async Task OnGetAsync(Guid userId)
    {
        PageUser = await _mediator.Send(new GetUserQuery(userId));
    }
}
