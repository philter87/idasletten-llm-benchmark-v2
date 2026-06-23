using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateModel : PageModel
{
    private readonly IMediator _mediator;
    public CreateModel(IMediator mediator) => _mediator = mediator;

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public int TeamSize { get; set; } = 2;
    [BindProperty] public int PointsToWin { get; set; } = 5;
    [BindProperty] public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.WinCount;
    [BindProperty] public int? MaxPlayerCount { get; set; }
    [BindProperty] public bool IsPublic { get; set; } = true;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        if (string.IsNullOrWhiteSpace(Name)) { ModelState.AddModelError(nameof(Name), "Name required"); return Page(); }
        var id = await _mediator.Send(new CreateTournamentCommand(Name, TeamSize, PointsToWin, ScoreSystem, MaxPlayerCount, IsPublic));
        // Create-and-Plan navigates to the matches page where "Plan several" lives.
        return RedirectToPage(action == "plan" ? "/Tournaments/Matches" : "/Tournaments/Detail", new { id });
    }
}