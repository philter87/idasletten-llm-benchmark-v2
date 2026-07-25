using Idasletten.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailModel(ISender sender) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid UserId { get; set; }

    public UserStats Stats { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var stats = await sender.Send(new GetUserStats(UserId));
        if (stats is null)
        {
            return NotFound();
        }

        Stats = stats;
        return Page();
    }
}
