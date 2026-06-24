using Idasletten.Features.Tournaments.Commands;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Idasletten.Pages.Tournaments;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateModel(IMediator mediator) => _mediator = mediator;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> ScoreSystemOptions { get; set; } = Enum.GetValues<ScoreSystem>()
        .Select(s => new SelectListItem(s.ToString(), s.ToString()))
        .ToList();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync(bool createAndPlan = false)
    {
        if (!ModelState.IsValid) return Page();

        var tournament = await _mediator.Send(new CreateTournamentCommand(
            Input.Name,
            Input.TeamSize,
            Input.PointsToWin,
            Enum.Parse<ScoreSystem>(Input.ScoreSystem),
            Input.MaxPlayerCount,
            Input.IsPublic,
            null, null
        ));

        if (createAndPlan)
            return RedirectToPage("/Tournaments/Matches", new { tournamentId = tournament.Id });

        return RedirectToPage("/Tournaments/Detail", new { tournamentId = tournament.Id });
    }

    public class InputModel
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 10)]
        public int TeamSize { get; set; } = 2;

        [Range(1, 99)]
        public int PointsToWin { get; set; } = 5;

        public string ScoreSystem { get; set; } = "Elo";

        public int? MaxPlayerCount { get; set; }
        public bool IsPublic { get; set; } = true;
    }
}
