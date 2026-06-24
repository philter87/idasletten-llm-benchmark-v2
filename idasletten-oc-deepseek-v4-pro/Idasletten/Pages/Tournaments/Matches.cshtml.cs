using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel : PageModel
{
    private readonly IMediator _mediator;

    public MatchesResult? Matches { get; set; }

    public MatchesModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid tournamentId)
    {
        Matches = await _mediator.Send(new GetMatchesForTournamentQuery(tournamentId));
    }

    public async Task<IActionResult> OnPostAddPlannedAsync(
        Guid tournamentId,
        string team1Player1Initials, string? team1Player2Initials,
        string team2Player1Initials, string? team2Player2Initials)
    {
        await _mediator.Send(new CreatePlannedMatchCommand(
            tournamentId, team1Player1Initials, team1Player2Initials,
            team2Player1Initials, team2Player2Initials));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostPlanSeveralAsync(
        Guid tournamentId, int gamesPerPlayer, bool fixedTeams, SeedingType seedingType)
    {
        await _mediator.Send(new PlanSeveralMatchesCommand(
            tournamentId, gamesPerPlayer, fixedTeams, seedingType));
        return RedirectToPage(new { tournamentId });
    }
}
