using Idasletten.Features.Players.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
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

    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    public List<TournamentPlayer> Players { get; set; } = new();
    public List<Tournament> SeedTournaments { get; set; } = new();
    public List<TournamentPlayer> SeedPlayers { get; set; } = new();
    public List<Guid> AddedSeedPlayerIds { get; set; } = new();

    [BindProperty]
    public Guid? SelectedSeedTournamentId { get; set; }

    public async Task OnGetAsync()
    {
        Players = await _mediator.Send(new GetPlayersQuery(TournamentId));
        SeedTournaments = await _mediator.Send(new GetTournamentsQuery());
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string initials, string? name)
    {
        await _mediator.Send(new AddPlayerCommand(tournamentId, initials, name));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetSeedTournamentAsync(Guid tournamentId, Guid seedTournamentId)
    {
        Players = await _mediator.Send(new GetPlayersQuery(TournamentId));
        SeedTournaments = await _mediator.Send(new GetTournamentsQuery());
        SeedPlayers = await _mediator.Send(new GetPlayersQuery(seedTournamentId));
        SelectedSeedTournamentId = seedTournamentId;
        return Page();
    }

    public async Task<IActionResult> OnPostAddSeedPlayerAsync(Guid tournamentId, Guid userId)
    {
        var seedPlayer = await _mediator.Send(new GetPlayersQuery(TournamentId));
        var user = seedPlayer.FirstOrDefault(p => p.UserId == userId);
        if (user != null)
        {
            await _mediator.Send(new AddPlayerCommand(tournamentId, user.User.Initials, user.User.Name));
        }
        return RedirectToPage();
    }
}
