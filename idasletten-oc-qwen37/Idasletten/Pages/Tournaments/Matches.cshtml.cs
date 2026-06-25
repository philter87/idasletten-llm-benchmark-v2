using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Models;
using MediatR;
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

    [BindProperty]
    public Guid TournamentId { get; set; }

    public List<TournamentMatch> PlannedMatches { get; set; } = new();
    public List<TournamentMatch> CompletedMatches { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        TournamentId = tournamentId;

        var allMatches = await _mediator.Send(new ListMatchesQuery(tournamentId));
        PlannedMatches = allMatches.Where(m => m.State == MatchState.Planned).ToList();
        CompletedMatches = allMatches.Where(m => m.State == MatchState.Done).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAddPlannedMatchAsync(Guid tournamentId, string team1Players, string team2Players)
    {
        TournamentId = tournamentId;

        var teamResults = new List<TeamResultDto>
        {
            new TeamResultDto(team1Players.Split(',').Select(s => s.Trim()).ToList(), 0),
            new TeamResultDto(team2Players.Split(',').Select(s => s.Trim()).ToList(), 0)
        };

        var matchCount = (await _mediator.Send(new ListMatchesQuery(tournamentId))).Count;
        await _mediator.Send(new CreateMatchCommand(tournamentId, matchCount + 1, teamResults));

        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostPlanMatchesAsync(Guid tournamentId, int gamesPerPlayer,
        string seedingType, bool fixedTeams)
    {
        TournamentId = tournamentId;

        var seeding = Enum.Parse<SeedingType>(seedingType);
        await _mediator.Send(new PlanMatchesCommand(tournamentId, gamesPerPlayer, fixedTeams, seeding, null));

        return RedirectToPage(new { tournamentId });
    }
}
