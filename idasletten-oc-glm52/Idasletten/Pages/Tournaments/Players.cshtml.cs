using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel : PageModel
{
    private readonly IMediator _mediator;
    public PlayersModel(IMediator mediator) => _mediator = mediator;

    public TournamentView Tournament { get; private set; } = null!;
    public List<PlayerView> Players { get; private set; } = new();
    public List<SeedPlayerView> SeedPlayers { get; private set; } = new();
    public bool HasSeed { get; private set; }
    public List<TournamentView> AllTournaments { get; private set; } = new();

    public async Task<IActionResult> OnGet(Guid id)
    {
        var t = await _mediator.Send(new GetTournamentQuery(id));
        if (t is null) return NotFound();
        Tournament = t;
        Players = await _mediator.Send(new ListTournamentPlayersQuery(id));
        AllTournaments = await _mediator.Send(new ListTournamentsQuery(IncludeHistorical: true));
        if (t.SeedTournamentId.HasValue)
        {
            SeedPlayers = await _mediator.Send(new GetPlayersFromTournamentQuery(t.SeedTournamentId.Value, id));
            HasSeed = true;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAdd(Guid id, string username, string? name, string? returnUrl)
    {
        await _mediator.Send(new Features.Players.Commands.AddPlayerCommand(id, username, name));
        return LocalRedirect(returnUrl ?? Url.Page("/Tournaments/Players", new { id })!);
    }

    public async Task<IActionResult> OnPostSetSeed(Guid id, Guid seedTournamentId)
    {
        await _mediator.Send(new Features.Tournaments.Commands.SetSeedTournamentCommand(id, seedTournamentId));
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddFromSeed(Guid id, List<Guid> userIds)
    {
        if (userIds is null || userIds.Count == 0) return RedirectToPage(new { id });
        var t = await _mediator.Send(new GetTournamentQuery(id));
        if (t?.SeedTournamentId is Guid seedId)
            await _mediator.Send(new Features.Players.Commands.AddPlayersFromTournamentCommand(id, seedId, userIds));
        return RedirectToPage(new { id });
    }
}