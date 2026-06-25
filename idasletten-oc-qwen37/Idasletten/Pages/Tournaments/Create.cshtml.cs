using Idasletten.Features.Tournaments.Commands;
using Idasletten.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Idasletten.Pages.Tournaments;

public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [BindProperty]
    public CreateTournamentInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var tournamentId = await _mediator.Send(new CreateTournamentCommand(
            Input.Name,
            Input.TeamSize,
            Input.PointsToWin,
            Input.ScoreSystem,
            Input.MaxPlayerCount,
            Input.IsPublic,
            null,
            null
        ));

        return RedirectToPage("/Tournaments/Detail", new { id = tournamentId });
    }

    public async Task<IActionResult> OnPostCreateAndPlanAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var tournamentId = await _mediator.Send(new CreateTournamentCommand(
            Input.Name,
            Input.TeamSize,
            Input.PointsToWin,
            Input.ScoreSystem,
            Input.MaxPlayerCount,
            Input.IsPublic,
            null,
            null
        ));

        return RedirectToPage("/Tournaments/Matches", new { tournamentId });
    }
}

public class CreateTournamentInput
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 10)]
    public int TeamSize { get; set; } = 2;

    [Range(1, 100)]
    public int PointsToWin { get; set; } = 5;

    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

    [Range(2, 100)]
    public int? MaxPlayerCount { get; set; }

    public bool IsPublic { get; set; } = true;
}
