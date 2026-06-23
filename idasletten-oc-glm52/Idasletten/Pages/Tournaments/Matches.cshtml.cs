using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel : PageModel
{
    private readonly IMediator _mediator;
    public MatchesModel(IMediator mediator) => _mediator = mediator;

    public TournamentMatchesView MatchesView { get; private set; } = null!;

    [BindProperty] public string Team1Initials { get; set; } = "";
    [BindProperty] public string Team2Initials { get; set; } = "";
    [BindProperty] public int GamesPerPlayer { get; set; } = 1;
    [BindProperty] public bool FixedTeam { get; set; }
    [BindProperty] public SeedingType SeedingType { get; set; } = SeedingType.Random;

    public async Task<IActionResult> OnGet(Guid id)
    {
        var mv = await _mediator.Send(new GetTournamentMatchesQuery(id));
        if (mv is null) return NotFound();
        MatchesView = mv;
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlanned(Guid id)
    {
        await _mediator.Send(new PlanMatchCommand(id, new List<List<string>>
        {
            ParseInitials(Team1Initials),
            ParseInitials(Team2Initials)
        }));
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostPlanSeveral(Guid id)
    {
        await _mediator.Send(new PlanSeveralMatchesCommand(id, GamesPerPlayer, FixedTeam, SeedingType));
        return RedirectToPage(new { id });
    }

    private static List<string> ParseInitials(string s)
        => s.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().ToUpperInvariant()).ToList();
}