using System.ComponentModel.DataAnnotations;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class CreateModel(IMediator mediator) : PageModel
{
    [BindProperty, Required]
    public string Name { get; set; } = "";

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

    public async Task<IActionResult> OnPost(string? next)
    {
        if (!ModelState.IsValid)
            return Page();

        var tournament = await mediator.Send(new CreateTournamentCommand(
            Name, TeamSize, PointsToWin, ScoreSystem, MaxPlayerCount, IsPublic));

        // "Create and Plan" continues to the matches page where planning happens.
        return next == "plan"
            ? Redirect($"/tournaments/{tournament.Id}/matches")
            : Redirect($"/tournaments/{tournament.Id}");
    }
}
