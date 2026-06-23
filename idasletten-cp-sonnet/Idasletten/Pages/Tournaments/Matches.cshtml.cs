using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Tournaments.Queries;
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

    public TournamentDto? Tournament { get; set; }
    public MatchesDto? Matches { get; set; }
    public IReadOnlyList<TournamentSummaryDto> AvailableSeedTournaments { get; set; } = [];

    public async Task OnGetAsync(Guid id)
    {
        Tournament = await _mediator.Send(new GetTournamentQuery(id));
        Matches = await _mediator.Send(new GetMatchesQuery(id));
        AvailableSeedTournaments = await _mediator.Send(new GetTournamentsQuery(false, false, false));
    }

    public async Task<IActionResult> OnPostPlanMatchAsync(Guid tournamentId, List<string> teams)
    {
        var teamPlayerLists = teams.Select(t =>
            (IReadOnlyList<string>)t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant()).ToList()
        ).ToList();

        await _mediator.Send(new PlanMatchCommand(tournamentId, teamPlayerLists));
        return RedirectToPage(new { id = tournamentId });
    }

    public async Task<IActionResult> OnPostPlanSeveralAsync(
        Guid tournamentId,
        int gamesPerPlayer,
        string seedingType,
        bool fixedTeams,
        Guid? seedTournamentId)
    {
        var seedingEnum = Enum.TryParse<SeedingType>(seedingType, out var parsed) ? parsed : SeedingType.Random;
        await _mediator.Send(new PlanSeveralMatchesCommand(tournamentId, gamesPerPlayer, fixedTeams, seedingEnum, seedTournamentId));
        return RedirectToPage(new { id = tournamentId });
    }
}
