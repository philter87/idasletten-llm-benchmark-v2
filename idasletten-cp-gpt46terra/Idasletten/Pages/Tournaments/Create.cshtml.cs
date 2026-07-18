using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateModel(ISender sender) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public IReadOnlyList<TournamentSummary> ParentTournaments { get; private set; } = [];
    public class InputModel
    {
        [BindProperty, Required] public string Name { get; set; } = "";
        [Range(1, 20)] public int TeamSize { get; set; } = 2;
        [Range(1, 100)] public int PointsToWin { get; set; } = 5;
        public int? MaxPlayerCount { get; set; }
        public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;
        public bool IsPublic { get; set; } = true;
        public Guid? ParentTournamentId { get; set; }
    }
    public async Task OnGetAsync() => ParentTournaments = await sender.Send(new GetTournamentsQuery(false, false));
    public async Task<IActionResult> OnPostAsync(bool plan = false)
    {
        if (!ModelState.IsValid) return Page();
        var id = await sender.Send(new CreateTournamentCommand(Input.Name, Input.MaxPlayerCount, Input.TeamSize, Input.PointsToWin, Input.ScoreSystem, Input.IsPublic, Input.ParentTournamentId));
        return RedirectToPage(plan ? "/Tournaments/Matches" : "/Tournaments/Details", new { tournamentId = id });
    }
}
