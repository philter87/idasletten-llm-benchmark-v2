using Idasletten.Features.Tournaments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(
        string name, int teamSize, int pointsToWin,
        Shared.Entities.ScoreSystem scoreSystem, int? maxPlayerCount,
        bool isPublic, bool planMatches = false)
    {
        var tournamentId = await _mediator.Send(new CreateTournamentCommand(
            name, teamSize, pointsToWin, scoreSystem, maxPlayerCount, isPublic));

        if (planMatches)
            return RedirectToPage("Matches", new { tournamentId });
        return RedirectToPage("Detail", new { tournamentId });
    }
}
