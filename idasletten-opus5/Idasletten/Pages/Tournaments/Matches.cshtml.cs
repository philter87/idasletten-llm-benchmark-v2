using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel(ISender sender) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    /// <summary>Set by "Opret og planlæg" so the planning dialog is open when the page loads.</summary>
    [BindProperty(SupportsGet = true)]
    public bool Plan { get; set; }

    public TournamentDetail Tournament { get; private set; } = null!;

    public MatchOverview Matches { get; private set; } = new([], []);

    public IReadOnlyList<ScoreboardRow> TournamentPlayers { get; private set; } = [];

    public IReadOnlyList<TournamentSummary> PreviousTournaments { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync() => await LoadAsync() ? Page() : NotFound();

    /// <summary>"Tilføj planlagt kamp" - one match written by initials.</summary>
    public async Task<IActionResult> OnPostPlanOneAsync(List<string> teamOne, List<string> teamTwo)
    {
        try
        {
            await sender.Send(new SaveMatch(
                TournamentId,
                Guid.NewGuid(),
                [new MatchTeamInput(teamOne, 0), new MatchTeamInput(teamTwo, 0)],
                AsPlanned: true));

            TempData["Message"] = "Kampen er sat i kalenderen.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { tournamentId = TournamentId });
    }

    /// <summary>"Planlæg flere kampe" - a whole schedule based on the chosen seeding.</summary>
    public async Task<IActionResult> OnPostPlanSeveralAsync(
        int gamesPerPlayer, bool fixedTeams, SeedingType seeding, Guid? seedTournamentId)
    {
        try
        {
            var created = await sender.Send(new PlanMatches(
                TournamentId, gamesPerPlayer, fixedTeams, seeding, seedTournamentId));

            TempData["Message"] = created == 0
                ? "Der er ikke spillere nok til at planlægge kampe endnu."
                : $"{created} kampe er planlagt.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { tournamentId = TournamentId });
    }

    private async Task<bool> LoadAsync()
    {
        var tournament = await sender.Send(new GetTournament(TournamentId));
        if (tournament is null)
        {
            return false;
        }

        Tournament = tournament;
        Matches = await sender.Send(new GetMatches(TournamentId));
        TournamentPlayers = await sender.Send(new GetScoreboard(TournamentId));
        PreviousTournaments = (await sender.Send(new GetTournaments()))
            .Where(summary => summary.Id != TournamentId)
            .ToList();

        return true;
    }
}
