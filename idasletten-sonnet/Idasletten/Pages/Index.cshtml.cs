using Idasletten.Features.Tournaments.Queries.GetAllTournaments;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel(ISender sender) : PageModel
{
    public List<Tournament> PublicTournaments { get; set; } = [];

    public async Task OnGetAsync()
    {
        var all = await sender.Send(new GetAllTournamentsQuery(IncludeChildren: false, IncludeArchived: false));
        PublicTournaments = all.Where(t => t.IsPublic).ToList();
    }
}
