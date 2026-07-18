using Idasletten.Features.Tournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel(ISender sender) : PageModel
{
    public IReadOnlyList<TournamentSummary> Tournaments { get; private set; } = [];
    public async Task OnGetAsync(bool showRounds = false) =>
        Tournaments = await sender.Send(new GetTournamentsQuery(false, showRounds));
}
