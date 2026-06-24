using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared.Enums;
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

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public int TeamSize { get; set; } = 2;

    [BindProperty]
    public int PointsToWin { get; set; } = 5;

    [BindProperty]
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

    [BindProperty]
    public int? MaxPlayerCount { get; set; }

    [BindProperty]
    public bool IsPublic { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var tournamentId = await _mediator.Send(new CreateTournamentCommand(
            Name, TeamSize, PointsToWin, ScoreSystem, MaxPlayerCount, IsPublic, null, null));

        if (Request.Form.ContainsKey("handler") && Request.Form["handler"] == "CreateAndPlan")
        {
            return RedirectToPage("/Tournaments/Matches", new { tournamentId });
        }

        return RedirectToPage("/Tournaments/Detail", new { tournamentId });
    }
}
