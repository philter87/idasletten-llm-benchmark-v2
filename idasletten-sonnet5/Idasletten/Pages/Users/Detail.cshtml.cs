using Idasletten.Features.Users.Queries.GetUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailModel(ISender sender) : PageModel
{
    public UserDetailDto UserDetail { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid userId)
    {
        var user = await sender.Send(new GetUserQuery(userId));
        if (user is null) return NotFound();

        UserDetail = user;
        return Page();
    }
}
