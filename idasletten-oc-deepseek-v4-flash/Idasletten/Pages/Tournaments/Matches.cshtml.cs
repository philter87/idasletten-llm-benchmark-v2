using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

    public List<TournamentMatch> PlannedMatches { get; set; } = new();
    public List<TournamentMatch> CompletedMatches { get; set; } = new();
    public List<Tournament> AvailableSeedTournaments { get; set; } = new();

    public async Task OnGetAsync()
    {
        var allMatches = await _mediator.Send(new GetMatchesQuery(TournamentId));
        PlannedMatches = allMatches.Where(m => m.State == MatchState.Planned).ToList();
        CompletedMatches = allMatches.Where(m => m.State == MatchState.Done).ToList();
        AvailableSeedTournaments = await _mediator.Send(new GetTournamentsQuery());
    }

    public async Task<IActionResult> OnPostAddPlannedAsync(Guid tournamentId,
        string team1Player1, string? team1Player2,
        string team2Player1, string? team2Player2)
    {
        var team1 = new List<string> { team1Player1 };
        if (!string.IsNullOrEmpty(team1Player2)) team1.Add(team1Player2);
        var team2 = new List<string> { team2Player1 };
        if (!string.IsNullOrEmpty(team2Player2)) team2.Add(team2Player2);

        await _mediator.Send(new CreateMatchCommand(tournamentId,
            new List<List<string>> { team1, team2 }));

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPlanSeveralAsync(Guid tournamentId,
        Guid? seedTournamentId, int gamesPerPlayer, bool fixedTeam, SeedingType seedingType)
    {
        // TODO: Implement multi-match planning logic
        return RedirectToPage();
    }
}
