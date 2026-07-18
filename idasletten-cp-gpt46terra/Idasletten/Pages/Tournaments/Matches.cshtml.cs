using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel(ISender sender) : PageModel
{
    public TournamentDetail? Tournament { get; private set; }
    public IReadOnlyList<MatchRow> Matches { get; private set; } = [];
    public IReadOnlyList<TournamentSummary> Seeds { get; private set; } = [];
    [BindProperty] public int GamesPerPlayer { get; set; } = 2;
    [BindProperty] public bool FixedTeams { get; set; }
    [BindProperty] public SeedingType SeedingType { get; set; } = SeedingType.Random;
    [BindProperty] public Guid? SeedTournamentId { get; set; }
    public async Task<IActionResult> OnGetAsync(Guid tournamentId) => await LoadAsync(tournamentId);
    public async Task<IActionResult> OnPostPlanAsync(Guid tournamentId)
    {
        if (!ModelState.IsValid) return await LoadAsync(tournamentId);
        await sender.Send(new PlanMatchesCommand(tournamentId, GamesPerPlayer, FixedTeams, SeedingType, SeedTournamentId));
        return RedirectToPage(new { tournamentId });
    }
    private async Task<IActionResult> LoadAsync(Guid tournamentId)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (Tournament is null) return NotFound();
        Matches = await sender.Send(new GetAllMatchesQuery(tournamentId));
        Seeds = await sender.Send(new GetTournamentsQuery(false, false));
        return Page();
    }
}
