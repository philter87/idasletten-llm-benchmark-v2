using Idasletten.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailsModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public UserDto? UserInfo { get; set; }
    public IReadOnlyList<UserTournamentStatDto> TournamentStats { get; set; } = [];

    public async Task OnGetAsync(Guid id)
    {
        var result = await _mediator.Send(new GetUserProfileQuery(id));
        UserInfo = result?.User;
        TournamentStats = result?.TournamentStats ?? [];
    }
}

