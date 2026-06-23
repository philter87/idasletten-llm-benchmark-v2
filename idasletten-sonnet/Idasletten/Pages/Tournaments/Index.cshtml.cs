using Idasletten.Features.Tournaments.Queries.GetAllTournaments;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class TournamentsIndexModel(ISender sender) : PageModel
{
    public List<Tournament> Tournaments { get; set; } = [];

    public async Task OnGetAsync([FromQuery] bool all = false)
    {
        Tournaments = await sender.Send(new GetAllTournamentsQuery(IncludeChildren: all, IncludeArchived: all));
    }
}
