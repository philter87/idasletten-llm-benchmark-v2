using Idasletten.Features.Matches.Commands.AddPlannedMatch;
using Idasletten.Features.Matches.Commands.PlanSeveralMatches;
using Idasletten.Features.Matches.Queries.GetTournamentMatches;
using Idasletten.Features.Players.Queries.GetSeedableTournaments;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel(ISender sender) : PageModel
{
    public TournamentDetailResult Tournament { get; private set; } = null!;
    public TournamentMatchesResult Matches { get; private set; } = null!;
    public IReadOnlyList<SeedableTournamentDto> SeedableTournaments { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public string PlannedTeam1Initials { get; set; } = string.Empty;

    [BindProperty]
    public string PlannedTeam2Initials { get; set; } = string.Empty;

    [BindProperty]
    public Guid? SeedTournamentId { get; set; }

    [BindProperty]
    public int GamesPerPlayer { get; set; } = 1;

    [BindProperty]
    public bool FixedTeams { get; set; }

    [BindProperty]
    public SeedingType SeedingType { get; set; } = SeedingType.Random;

    public async Task<IActionResult> OnGetAsync()
    {
        var tournament = await sender.Send(new GetTournamentDetailQuery(Id));
        if (tournament is null)
        {
            return NotFound();
        }
        Tournament = tournament;
        Matches = await sender.Send(new GetTournamentMatchesQuery(Id));
        SeedableTournaments = await sender.Send(new GetSeedableTournamentsQuery(Id));
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlannedMatchAsync()
    {
        var team1 = SplitInitials(PlannedTeam1Initials);
        var team2 = SplitInitials(PlannedTeam2Initials);
        if (team1.Count > 0 && team2.Count > 0)
        {
            await sender.Send(new AddPlannedMatchCommand(Id, [team1, team2]));
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostPlanSeveralAsync()
    {
        try
        {
            await sender.Send(new PlanSeveralMatchesCommand(Id, SeedTournamentId, GamesPerPlayer, FixedTeams, SeedingType));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage(new { id = Id });
    }

    private static List<string> SplitInitials(string input) => input
        .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}
