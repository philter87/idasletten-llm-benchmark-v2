using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages;

public class IndexModel(ISender sender) : PageModel
{
    public IReadOnlyList<TournamentSummary> Tournaments { get; private set; } = [];

    public async Task OnGetAsync()
    {
        // The front page only shows the tournaments that are public and still running.
        Tournaments = await sender.Send(new GetTournaments(OnlyPublic: true, IncludeArchived: false));
    }
}
