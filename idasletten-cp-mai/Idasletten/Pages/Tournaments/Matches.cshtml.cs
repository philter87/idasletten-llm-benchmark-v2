using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel : PageModel
{
    private readonly IMediator _mediator;

    public MatchesModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    public MatchesDto Matches { get; set; } = new();

    [BindProperty]
    public List<string> PlanMatchInitials { get; set; } = [];

    [BindProperty]
    public int GamesPerPlayer { get; set; } = 2;

    [BindProperty]
    public bool FixedTeam { get; set; }

    [BindProperty]
    public SeedingType Seeding { get; set; } = SeedingType.Random;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Matches = await _mediator.Send(new GetMatchesQuery(TournamentId), cancellationToken);
    }

    public async Task<IActionResult> OnPostPlanSingleAsync(CancellationToken cancellationToken)
    {
        var teams = PlanMatchInitials
            .Select(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().ToUpperInvariant()).Where(x => x.Length > 0).ToList())
            .Where(list => list.Count > 0)
            .ToList();

        if (teams.Count >= 2)
        {
            await _mediator.Send(new PlanMatchCommand(TournamentId, teams), cancellationToken);
        }

        return RedirectToPage(new { tournamentId = TournamentId });
    }

    public async Task<IActionResult> OnPostPlanSeveralAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new PlanSeveralMatchesCommand(TournamentId, GamesPerPlayer, FixedTeam, Seeding), cancellationToken);
        return RedirectToPage(new { tournamentId = TournamentId });
    }
}
