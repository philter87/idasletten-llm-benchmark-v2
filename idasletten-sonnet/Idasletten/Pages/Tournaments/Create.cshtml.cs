using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class CreateTournamentModel(ISender sender) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Name { get; set; } = string.Empty;
        public int TeamSize { get; set; } = 2;
        public int PointsToWin { get; set; } = 5;
        public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
        public int? MaxPlayerCount { get; set; }
        public bool IsPublic { get; set; } = true;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var id = await sender.Send(new CreateTournamentCommand(
            Input.Name,
            Input.TeamSize,
            Input.PointsToWin,
            Input.ScoreSystem,
            Input.MaxPlayerCount,
            Input.IsPublic
        ));

        return RedirectToPage("/Tournaments/Detail", new { tournamentId = id });
    }
}
