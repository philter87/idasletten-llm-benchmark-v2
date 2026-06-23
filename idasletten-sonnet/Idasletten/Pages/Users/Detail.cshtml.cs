using Idasletten.Features.Users.Queries.GetUser;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Users;

public class UserDetailModel(ISender sender) : PageModel
{
    public User? AppUser { get; set; }

    public async Task OnGetAsync(Guid userId)
    {
        AppUser = await sender.Send(new GetUserQuery(userId));
    }
}
