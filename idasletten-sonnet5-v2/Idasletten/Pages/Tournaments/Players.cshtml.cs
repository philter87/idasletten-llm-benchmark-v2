using Idasletten.Features.Players.Commands.AddExistingUserToTournament;
using Idasletten.Features.Players.Commands.AddPlayerToTournament;
using Idasletten.Features.Players.Queries.GetSeedableTournaments;
using Idasletten.Features.Players.Queries.GetSeedTournamentPlayers;
using Idasletten.Features.Players.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments.Commands.SetSeedTournament;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel(ISender sender) : PageModel
{
    public TournamentDetailResult Tournament { get; private set; } = null!;
    public IReadOnlyList<TournamentPlayerDto> Players { get; private set; } = [];
    public IReadOnlyList<SeedableTournamentDto> SeedableTournaments { get; private set; } = [];
    public IReadOnlyList<SeedTournamentPlayerDto>? SeedPlayers { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public string AddPlayerUsername { get; set; } = string.Empty;

    [BindProperty]
    public string? AddPlayerName { get; set; }

    [BindProperty]
    public Guid SeedTournamentId { get; set; }

    [BindProperty]
    public Guid AddUserId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var tournament = await sender.Send(new GetTournamentDetailQuery(Id));
        if (tournament is null)
        {
            return NotFound();
        }
        Tournament = tournament;
        Players = await sender.Send(new GetTournamentPlayersQuery(Id));

        if (tournament.SeedTournamentId is { } seedId)
        {
            SeedPlayers = await sender.Send(new GetSeedTournamentPlayersQuery(seedId, Id));
        }
        else
        {
            SeedableTournaments = await sender.Send(new GetSeedableTournamentsQuery(Id));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync()
    {
        if (!string.IsNullOrWhiteSpace(AddPlayerUsername))
        {
            try
            {
                await sender.Send(new AddPlayerToTournamentCommand(Id, AddPlayerUsername.Trim(), AddPlayerName?.Trim()));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostSetSeedTournamentAsync()
    {
        await sender.Send(new SetSeedTournamentCommand(Id, SeedTournamentId));
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddFromSeedAsync()
    {
        try
        {
            await sender.Send(new AddExistingUserToTournamentCommand(Id, AddUserId));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage(new { id = Id });
    }
}
