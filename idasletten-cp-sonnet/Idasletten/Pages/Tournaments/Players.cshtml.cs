using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel : PageModel
{
    private readonly IMediator _mediator;

    public PlayersModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public TournamentDto? Tournament { get; set; }
    public IReadOnlyList<TournamentSummaryDto> AvailableSeedTournaments { get; set; } = [];
    public TournamentDto? SelectedSeedTournament { get; set; }
    public IReadOnlyList<PlayerDto> SeedPlayers { get; set; } = [];

    public async Task OnGetAsync(Guid id, Guid? seedId)
    {
        Tournament = await _mediator.Send(new GetTournamentQuery(id));
        AvailableSeedTournaments = (await _mediator.Send(new GetTournamentsQuery(true, true, true)))
            .Where(t => t.Id != id)
            .ToList();

        if (seedId.HasValue)
        {
            SelectedSeedTournament = await _mediator.Send(new GetTournamentQuery(seedId.Value));
            SeedPlayers = SelectedSeedTournament?.Players
                .OrderByDescending(p => p.Score)
                .ToList() ?? [];
        }
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string username, string? name)
    {
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, username.ToUpperInvariant(), name));
        return RedirectToPage(new { id = tournamentId });
    }

    public IActionResult OnPostSelectSeed(Guid tournamentId, Guid seedTournamentId)
    {
        return RedirectToPage(new { id = tournamentId, seedId = seedTournamentId });
    }

    public async Task<IActionResult> OnPostAddFromSeedAsync(Guid tournamentId, string username)
    {
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, username, null));
        return RedirectToPage(new { id = tournamentId });
    }
}
