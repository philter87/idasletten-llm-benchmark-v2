using Idasletten.Features.Players.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel : PageModel
{
    private readonly IMediator _mediator;
    public PlayersModel(IMediator mediator) => _mediator = mediator;

    public TournamentDetail Tournament { get; private set; } = null!;
    public IReadOnlyList<PlayerRow> Players { get; private set; } = new List<PlayerRow>();
    public IReadOnlyList<TournamentListItem> PreviousTournaments { get; private set; } = new List<TournamentListItem>();

    public Guid? SelectedSeedId { get; private set; }
    public IReadOnlyList<SeedCandidate> SeedPlayers { get; private set; } = new List<SeedCandidate>();

    [BindProperty] public string? Initials { get; set; }
    [BindProperty] public string? PlayerName { get; set; }

    public async Task<IActionResult> OnGet(Guid tournamentId, Guid? seedId)
    {
        if (!await LoadAsync(tournamentId, seedId)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayer(Guid tournamentId)
    {
        if (!string.IsNullOrWhiteSpace(Initials))
            await _mediator.Send(new AddPlayerCommand(tournamentId, Initials!.Trim(), PlayerName));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostAddFromSeed(Guid tournamentId, Guid seedId, Guid userId)
    {
        await _mediator.Send(new AddPlayerFromTournamentCommand(tournamentId, seedId, userId));
        return RedirectToPage(new { tournamentId, seedId });
    }

    private async Task<bool> LoadAsync(Guid tournamentId, Guid? seedId)
    {
        var detail = await _mediator.Send(new GetTournamentDetailQuery(tournamentId));
        if (detail is null) return false;
        Tournament = detail;
        Players = await _mediator.Send(new GetPlayersQuery(tournamentId));

        // Prefer the tournament's stored seed; otherwise whatever was picked from the menu.
        SelectedSeedId = detail.SeedTournamentId ?? seedId;

        PreviousTournaments = (await _mediator.Send(new ListTournamentsQuery(IncludeChildren: true)))
            .Where(t => t.Id != tournamentId).ToList();

        if (SelectedSeedId is { } sid)
            SeedPlayers = await _mediator.Send(new GetSeedCandidatesQuery(tournamentId, sid));

        return true;
    }
}
