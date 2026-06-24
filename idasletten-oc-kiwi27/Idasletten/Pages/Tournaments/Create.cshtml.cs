using Idasletten.Features.Tournaments;
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

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? action, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var id = await _mediator.Send(new CreateTournamentCommand(
            Name, TeamSize, PointsToWin, ScoreSystem, MaxPlayerCount, IsPublic), cancellationToken);

        if (action == "create-and-plan")
        {
            return RedirectToPage("/Tournaments/Matches", new { tournamentId = id });
        }

        return RedirectToPage("/Tournaments/Detail", new { tournamentId = id });
    }
}
