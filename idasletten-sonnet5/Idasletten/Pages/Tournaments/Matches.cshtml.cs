using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands.PlanMultipleMatches;
using Idasletten.Features.Matches.Queries.GetAllMatches;
using Idasletten.Features.Matches.Commands.SaveMatch;
using Idasletten.Features.Matches.Commands.CreatePlannedMatch;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using Idasletten.Features.Tournaments.Queries.GetTournaments;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel(ISender sender) : PageModel
{
    public TournamentDto Tournament { get; private set; } = null!;
    public AllMatchesDto AllMatches { get; private set; } = null!;
    public IReadOnlyList<TournamentSummaryDto> PossibleSeedTournaments { get; private set; } = [];

    [BindProperty]
    public string PlayerUsernamesCsv { get; set; } = string.Empty;

    [BindProperty]
    public int GamesPerPlayer { get; set; } = 1;

    [BindProperty]
    public bool FixedTeams { get; set; }

    [BindProperty]
    public SeedingType SeedingType { get; set; } = SeedingType.Random;

    [BindProperty]
    public Guid? SeedTournamentId { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        var tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (tournament is null) return NotFound();

        Tournament = tournament;
        AllMatches = await sender.Send(new GetAllMatchesQuery(tournamentId));
        PossibleSeedTournaments = await sender.Send(new GetTournamentsQuery());
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlannedMatchAsync(Guid tournamentId)
    {
        var matchId = await sender.Send(new CreatePlannedMatchCommand(tournamentId));
        var teams = PlayerUsernamesCsv
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(teamCsv => new TeamInput(
                teamCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), 0))
            .Where(t => t.Initials.Count > 0)
            .ToList();

        if (teams.Count > 0)
        {
            await sender.Send(new SaveMatchCommand(matchId, tournamentId, teams, RecordResult: false));
        }

        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostPlanMultipleAsync(Guid tournamentId)
    {
        await sender.Send(new PlanMultipleMatchesCommand(
            tournamentId, GamesPerPlayer, FixedTeams, SeedingType, SeedTournamentId));
        return RedirectToPage(new { tournamentId });
    }
}
