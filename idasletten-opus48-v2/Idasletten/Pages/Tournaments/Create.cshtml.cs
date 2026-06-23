using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;
    public CreateModel(IMediator mediator) => _mediator = mediator;

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public int? MaxPlayerCount { get; set; }
    [BindProperty] public int TeamSize { get; set; } = 2;
    [BindProperty] public int PointsToWin { get; set; } = 5;
    [BindProperty] public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
    [BindProperty] public bool IsPublic { get; set; } = true;

    public void OnGet() { }

    public Task<IActionResult> OnPostCreate() => CreateAsync(plan: false);
    public Task<IActionResult> OnPostCreateAndPlan() => CreateAsync(plan: true);

    private async Task<IActionResult> CreateAsync(bool plan)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "Name is required.");
            return Page();
        }

        var id = await _mediator.Send(new CreateTournamentCommand(
            Name.Trim(), TeamSize, PointsToWin, ScoreSystem, MaxPlayerCount, IsPublic));

        // "Create and Plan" goes straight to the matches page where planning lives.
        return plan
            ? RedirectToPage("/Tournaments/Matches", new { tournamentId = id })
            : RedirectToPage("/Tournaments/Detail", new { id });
    }
}
