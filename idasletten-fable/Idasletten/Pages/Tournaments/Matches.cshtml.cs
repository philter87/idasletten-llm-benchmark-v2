using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel(IMediator mediator) : PageModel
{
    public MatchesResult Matches { get; private set; } = null!;

    public async Task<IActionResult> OnGet(Guid tournamentId)
    {
        var matches = await mediator.Send(new GetMatchesQuery(tournamentId));
        if (matches is null)
            return NotFound();
        Matches = matches;
        return Page();
    }

    public async Task<IActionResult> OnPostPlanMatch(Guid tournamentId, string team1, string team2)
    {
        try
        {
            await mediator.Send(new PlanMatchCommand(tournamentId, [SplitInitials(team1), SplitInitials(team2)]));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect($"/tournaments/{tournamentId}/matches");
    }

    public async Task<IActionResult> OnPostPlanSeveral(
        Guid tournamentId, int gamesPerPlayer, bool fixedTeams, SeedingType seedingType, Guid? seedTournamentId)
    {
        try
        {
            await mediator.Send(new PlanSeveralMatchesCommand(
                tournamentId, gamesPerPlayer, fixedTeams, seedingType, seedTournamentId));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return Redirect($"/tournaments/{tournamentId}/matches");
    }

    private static List<string> SplitInitials(string input) =>
        input.Split([',', ' ', '+'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
