using Idasletten.Features.Tournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel(ISender sender) : PageModel
{
    public TournamentDetail? Tournament { get; private set; }
    public IReadOnlyList<TournamentSummary> PreviousTournaments { get; private set; } = [];
    public IReadOnlyList<PlayerRow> SeedPlayers { get; private set; } = [];
    [BindProperty] public string Initials { get; set; } = "";
    [BindProperty] public string? Name { get; set; }
    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid? seedTournamentId) => await LoadAsync(tournamentId, seedTournamentId);
    public async Task<IActionResult> OnPostAddAsync(Guid tournamentId)
    {
        if (!string.IsNullOrWhiteSpace(Initials))
            await sender.Send(new AddPlayerCommand(tournamentId, Initials, Name));
        return RedirectToPage(new { tournamentId });
    }
    private async Task<IActionResult> LoadAsync(Guid tournamentId, Guid? seedTournamentId = null)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (Tournament is null) return NotFound();
        PreviousTournaments = (await sender.Send(new GetTournamentsQuery(false, false))).Where(x => x.Id != tournamentId).ToList();
        if (seedTournamentId is { } seedId)
            SeedPlayers = (await sender.Send(new GetTournamentQuery(seedId)))?.Players ?? [];
        return Page();
    }
}
