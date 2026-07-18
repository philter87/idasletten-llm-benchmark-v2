using Idasletten.Features.Tournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailsModel(ISender sender) : PageModel
{
    public UserStats? UserStats { get; private set; }
    public async Task<IActionResult> OnGetAsync(Guid userId)
    {
        UserStats = await sender.Send(new GetUserStatsQuery(userId));
        return UserStats is null ? NotFound() : Page();
    }
}
