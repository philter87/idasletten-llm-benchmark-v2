using Idasletten.Features.Tournaments.Queries.GetTournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel(ISender sender) : PageModel
{
    public IReadOnlyList<TournamentSummaryDto> Tournaments { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Tournaments = await sender.Send(new GetTournamentsQuery());
    }
}
