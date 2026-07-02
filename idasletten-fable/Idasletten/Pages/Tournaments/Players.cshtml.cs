using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel(IMediator mediator) : PageModel
{
    public TournamentPlayersResult Players { get; private set; } = null!;

    public async Task<IActionResult> OnGet(Guid tournamentId)
    {
        var players = await mediator.Send(new GetTournamentPlayersQuery(tournamentId));
        if (players is null)
            return NotFound();
        Players = players;
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayer(Guid tournamentId, string initials, string? name)
    {
        try
        {
            await mediator.Send(new AddPlayerToTournamentCommand(tournamentId, initials, name));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect($"/tournaments/{tournamentId}/players");
    }

    public async Task<IActionResult> OnPostSelectSeed(Guid tournamentId, Guid seedTournamentId)
    {
        try
        {
            await mediator.Send(new SetSeedTournamentCommand(tournamentId, seedTournamentId));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect($"/tournaments/{tournamentId}/players");
    }

    public async Task<IActionResult> OnPostAddSeedPlayer(Guid tournamentId, string initials)
    {
        try
        {
            await mediator.Send(new AddPlayerToTournamentCommand(tournamentId, initials));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect($"/tournaments/{tournamentId}/players");
    }

    public async Task<IActionResult> OnPostRemovePlayer(Guid tournamentId, Guid userId)
    {
        try
        {
            await mediator.Send(new RemovePlayerFromTournamentCommand(tournamentId, userId));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect($"/tournaments/{tournamentId}/players");
    }
}
