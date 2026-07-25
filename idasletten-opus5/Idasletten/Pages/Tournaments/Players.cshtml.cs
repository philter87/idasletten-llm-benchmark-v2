using Idasletten.Features.Players.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel(ISender sender) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    /// <summary>The previous tournament we are picking players from.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? SourceTournamentId { get; set; }

    public TournamentDetail Tournament { get; private set; } = null!;

    public IReadOnlyList<ScoreboardRow> Players { get; private set; } = [];

    public IReadOnlyList<SeedPlayerRow> SourcePlayers { get; private set; } = [];

    public IReadOnlyList<TournamentSummary> PreviousTournaments { get; private set; } = [];

    public string? SourceTournamentName { get; private set; }

    public async Task<IActionResult> OnGetAsync() => await LoadAsync() ? Page() : NotFound();

    public async Task<IActionResult> OnPostAddPlayerAsync(string initials, string? name)
    {
        try
        {
            await sender.Send(new AddPlayerToTournament(TournamentId, initials, name));
            TempData["Message"] = $"{initials.ToUpperInvariant()} er tilføjet.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { tournamentId = TournamentId, sourceTournamentId = SourceTournamentId });
    }

    /// <summary>
    /// Picking a previous tournament also makes it the seed tournament, unless this tournament is a
    /// round of another tournament - then it may not be seeded.
    /// </summary>
    public async Task<IActionResult> OnPostSelectSourceAsync(Guid sourceTournamentId)
    {
        var tournament = await sender.Send(new GetTournament(TournamentId));

        if (tournament is { CanBeSeeded: true, SeedTournamentId: null })
        {
            await sender.Send(new SetSeedTournament(TournamentId, sourceTournamentId));
        }

        return RedirectToPage(new { tournamentId = TournamentId, sourceTournamentId });
    }

    public async Task<IActionResult> OnPostAddFromTournamentAsync(Guid sourceTournamentId, Guid userId)
    {
        try
        {
            await sender.Send(new AddPlayersFromTournament(TournamentId, sourceTournamentId, [userId]));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { tournamentId = TournamentId, sourceTournamentId });
    }

    public async Task<IActionResult> OnPostRemovePlayerAsync(Guid tournamentPlayerId)
    {
        try
        {
            await sender.Send(new RemovePlayerFromTournament(TournamentId, tournamentPlayerId));
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { tournamentId = TournamentId, sourceTournamentId = SourceTournamentId });
    }

    /// <summary>The row in this tournament for a user we already added - used by the minus button.</summary>
    public Guid? TournamentPlayerIdFor(Guid userId) =>
        Players.FirstOrDefault(player => player.UserId == userId)?.TournamentPlayerId;

    private async Task<bool> LoadAsync()
    {
        var tournament = await sender.Send(new GetTournament(TournamentId));
        if (tournament is null)
        {
            return false;
        }

        Tournament = tournament;
        Players = await sender.Send(new GetScoreboard(TournamentId));
        PreviousTournaments = (await sender.Send(new GetTournaments()))
            .Where(summary => summary.Id != TournamentId)
            .ToList();

        SourceTournamentId ??= tournament.SeedTournamentId ?? tournament.ParentTournamentId;

        if (SourceTournamentId is { } sourceId)
        {
            SourcePlayers = await sender.Send(new GetPlayersFromTournament(sourceId, TournamentId));
            SourceTournamentName = (await sender.Send(new GetTournament(sourceId)))?.Name;
        }

        return true;
    }
}
