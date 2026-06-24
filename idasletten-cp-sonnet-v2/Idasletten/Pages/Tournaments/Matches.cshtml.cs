using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel : PageModel
{
    private readonly IMediator _mediator;

    public MatchesModel(IMediator mediator) => _mediator = mediator;

    public Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
    public List<TournamentMatch> PlannedMatches { get; set; } = [];
    public List<TournamentMatch> CompletedMatches { get; set; } = [];
    public List<Tournament> AllTournaments { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        TournamentId = tournamentId;
        Tournament = await _mediator.Send(new GetTournamentByIdQuery(tournamentId));
        if (Tournament == null) return NotFound();

        var matches = await _mediator.Send(new GetMatchesForTournamentQuery(tournamentId));
        PlannedMatches = matches.Planned;
        CompletedMatches = matches.Completed;

        AllTournaments = await _mediator.Send(new GetTournamentsQuery(IncludeArchived: true));
        AllTournaments = AllTournaments.Where(t => t.Id != tournamentId).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAddPlannedAsync(Guid tournamentId, string team1Initials, string team2Initials)
    {
        var t1 = ParseInitials(team1Initials);
        var t2 = ParseInitials(team2Initials);

        await _mediator.Send(new CreatePlannedMatchCommand(tournamentId, t1, t2));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostPlanSeveralAsync(
        Guid tournamentId,
        int gamesPerPlayer,
        string seedingType,
        bool fixedTeams,
        Guid? seedTournamentId)
    {
        var seeding = Enum.TryParse<SeedingType>(seedingType, out var s) ? s : SeedingType.Random;

        await _mediator.Send(new PlanSeveralMatchesCommand(
            tournamentId,
            gamesPerPlayer,
            fixedTeams,
            seeding,
            seedTournamentId
        ));

        return RedirectToPage(new { tournamentId });
    }

    private static List<string> ParseInitials(string input) =>
        input.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
             .Select(s => s.Trim())
             .Where(s => !string.IsNullOrEmpty(s))
             .ToList();
}
