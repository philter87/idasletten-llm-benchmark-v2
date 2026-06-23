using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel(IMediator mediator) : PageModel
{
    public MatchList Matches { get; private set; } = null!;
    [BindProperty] public string Team1Initials { get; set; } = "";
    [BindProperty] public string Team2Initials { get; set; } = "";
    [BindProperty] public int GamesPerPlayer { get; set; } = 1;
    [BindProperty] public bool FixedTeams { get; set; }
    [BindProperty] public string SeedingType { get; set; } = "Random";

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        var matches = await mediator.Send(new GetMatchListQuery(tournamentId));
        if (matches is null) return NotFound();
        Matches = matches;
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlannedAsync(Guid tournamentId)
    {
        await mediator.Send(new CreatePlannedMatchCommand(tournamentId, Split(Team1Initials), Split(Team2Initials)));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostPlanSeveralAsync(Guid tournamentId)
    {
        await mediator.Send(new PlanSeveralMatchesCommand(tournamentId, GamesPerPlayer, FixedTeams, SeedingType));
        return RedirectToPage(new { tournamentId });
    }

    private static IReadOnlyList<string> Split(string value) => value.Split([',', ' ', '+', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
