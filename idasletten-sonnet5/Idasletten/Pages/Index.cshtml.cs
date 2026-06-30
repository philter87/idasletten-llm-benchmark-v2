using Idasletten.Features.Tournaments.Queries.GetTournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel(ISender sender) : PageModel
{
    public IReadOnlyList<TournamentSummaryDto> PublicTournaments { get; private set; } = [];

    public async Task OnGetAsync()
    {
        PublicTournaments = await sender.Send(new GetTournamentsQuery(IsPublic: true, IncludeArchived: false));
    }
}
