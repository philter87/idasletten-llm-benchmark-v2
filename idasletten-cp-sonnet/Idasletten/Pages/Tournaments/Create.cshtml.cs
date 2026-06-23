using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Entities;
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

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string name,
        int teamSize,
        int pointsToWin,
        string scoreSystem,
        int? maxPlayerCount,
        bool isPublic,
        string? redirectTo)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "Navn er påkrævet.";
            return Page();
        }

        if (!Enum.TryParse<ScoreSystem>(scoreSystem, out var parsedScoreSystem))
            parsedScoreSystem = ScoreSystem.Elo;

        var id = await _mediator.Send(new CreateTournamentCommand(
            name, teamSize, pointsToWin, parsedScoreSystem, maxPlayerCount, isPublic, null, null));

        return redirectTo == "plan"
            ? RedirectToPage("/Tournaments/Matches", new { id })
            : RedirectToPage("/Tournaments/Details", new { id });
    }
}
