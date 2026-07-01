using Idasletten.Features.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class DetailsModel(ISender sender) : PageModel
{
    public UserProfileResult Profile { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var profile = await sender.Send(new GetUserProfileQuery(id));
        if (profile is null)
        {
            return NotFound();
        }
        Profile = profile;
        return Page();
    }
}
