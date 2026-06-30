using Idasletten.Features.TournamentPlayers.Commands.AddPlayerToTournament;
using Idasletten.Features.TournamentPlayers.Commands.RemovePlayerFromTournament;
using Idasletten.Features.TournamentPlayers.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments.Commands.SetSeedTournament;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using Idasletten.Features.Tournaments.Queries.GetTournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel(ISender sender) : PageModel
{
    public TournamentDto Tournament { get; private set; } = null!;
    public IReadOnlyList<TournamentPlayerDto> Players { get; private set; } = [];
    public IReadOnlyList<TournamentSummaryDto> CandidateSeedTournaments { get; private set; } = [];
    public TournamentDto? SeedTournament { get; private set; }
    public IReadOnlyList<TournamentPlayerDto> SeedTournamentPlayers { get; private set; } = [];

    [BindProperty]
    public string PlayerUsername { get; set; } = string.Empty;

    [BindProperty]
    public string? PlayerName { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        var tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (tournament is null) return NotFound();

        Tournament = tournament;
        Players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));

        if (tournament.SeedTournamentId is { } seedId)
        {
            SeedTournament = await sender.Send(new GetTournamentQuery(seedId));
            SeedTournamentPlayers = await sender.Send(new GetTournamentPlayersQuery(seedId));
        }
        else
        {
            var allTournaments = await sender.Send(new GetTournamentsQuery());
            CandidateSeedTournaments = allTournaments.Where(t => t.Id != tournamentId).ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId)
    {
        if (!string.IsNullOrWhiteSpace(PlayerUsername))
        {
            await sender.Send(new AddPlayerToTournamentCommand(tournamentId, PlayerUsername, PlayerName));
        }
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostSetSeedTournamentAsync(Guid tournamentId, Guid seedTournamentId)
    {
        await sender.Send(new SetSeedTournamentCommand(tournamentId, seedTournamentId));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostAddFromSeedAsync(Guid tournamentId, string username)
    {
        await sender.Send(new AddPlayerToTournamentCommand(tournamentId, username));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostRemoveFromSeedAsync(Guid tournamentId, Guid userId)
    {
        await sender.Send(new RemovePlayerFromTournamentCommand(tournamentId, userId));
        return RedirectToPage(new { tournamentId });
    }
}
