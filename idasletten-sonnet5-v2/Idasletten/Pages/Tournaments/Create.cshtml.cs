using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class CreateModel(ISender sender) : PageModel
{
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
    public bool IsPublic { get; set; } = true;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(bool andPlan = false)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "Name is required.");
            return Page();
        }

        var tournamentId = await sender.Send(new CreateTournamentCommand(Name, TeamSize, PointsToWin, ScoreSystem, MaxPlayerCount, IsPublic));

        return andPlan
            ? RedirectToPage("/Tournaments/Matches", new { id = tournamentId })
            : RedirectToPage("/Tournaments/Details", new { id = tournamentId });
    }
}
