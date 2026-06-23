using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateModel(IMediator mediator) : PageModel
{
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public int TeamSize { get; set; } = 2;
    [BindProperty] public int PointsToWin { get; set; } = 5;
    [BindProperty] public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
    [BindProperty] public int? MaxPlayerCount { get; set; }
    [BindProperty] public bool IsPublic { get; set; } = true;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? intent)
    {
        if (string.IsNullOrWhiteSpace(Name)) ModelState.AddModelError(nameof(Name), "Name is required.");
        if (!ModelState.IsValid) return Page();
        var id = await mediator.Send(new CreateTournamentCommand(Name, TeamSize, PointsToWin, ScoreSystem, MaxPlayerCount, IsPublic));
        return intent == "plan" ? RedirectToPage("/Tournaments/Matches", new { tournamentId = id }) : RedirectToPage("/Tournaments/Details", new { tournamentId = id });
    }
}
