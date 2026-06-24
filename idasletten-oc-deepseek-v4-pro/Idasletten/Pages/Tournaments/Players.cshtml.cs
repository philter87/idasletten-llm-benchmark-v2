using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel : PageModel
{
    private readonly IMediator _mediator;

    public Shared.Entities.Tournament? Tournament { get; set; }
    public List<Shared.Entities.Tournament> SeedTournaments { get; set; } = [];

    public PlayersModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid tournamentId)
    {
        Tournament = await _mediator.Send(new GetTournamentByIdQuery(tournamentId));
        SeedTournaments = await _mediator.Send(new GetTournamentsQuery());
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string initials, string? name)
    {
        await _mediator.Send(new AddPlayerToTournamentCommand(tournamentId, initials, name));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostSeedFromTournamentAsync(Guid tournamentId, Guid seedTournamentId)
    {
        var seedTournament = await _mediator.Send(new GetTournamentByIdQuery(seedTournamentId));
        if (seedTournament != null)
        {
            foreach (var seedPlayer in seedTournament.Players.OrderByDescending(p => p.Score))
            {
                await _mediator.Send(new AddPlayerToTournamentCommand(
                    tournamentId, seedPlayer.User.Username, seedPlayer.User.Name));
            }
        }
        return RedirectToPage(new { tournamentId });
    }
}
