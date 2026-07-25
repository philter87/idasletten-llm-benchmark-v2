using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class IndexModel(ISender sender) : PageModel
{
    /// <summary>Later rounds are hidden by default - they belong to their parent tournament.</summary>
    [BindProperty(SupportsGet = true)]
    public bool ShowRounds { get; set; }

    public IReadOnlyList<TournamentSummary> Tournaments { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Tournaments = await sender.Send(new GetTournaments(
            OnlyPublic: false, IncludeArchived: true, IncludeRounds: ShowRounds));
    }
}
