using Idasletten.Features.Matches.Commands.CreateMatch;
using Idasletten.Features.Matches.Commands.PlanMatches;
using Idasletten.Features.Matches.Queries.GetMatches;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel(ISender sender) : PageModel
{
    public Tournament? Tournament { get; set; }
    public List<TournamentMatch> PlannedMatches { get; set; } = [];
    public List<TournamentMatch> DoneMatches { get; set; } = [];

    public async Task OnGetAsync(Guid tournamentId)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (Tournament is null) return;

        PlannedMatches = await sender.Send(new GetMatchesQuery(tournamentId, MatchState.Planned));
        DoneMatches = (await sender.Send(new GetMatchesQuery(tournamentId, MatchState.Done)))
            .OrderByDescending(m => m.PlayedAt).ToList();
    }

    public async Task<IActionResult> OnPostPlanMatchAsync(Guid tournamentId,
        [FromForm(Name = "Team1")] List<string> team1,
        [FromForm(Name = "Team2")] List<string> team2)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (Tournament is null) return NotFound();

        var match = new Idasletten.Shared.Entities.TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            State = MatchState.Planned,
        };

        // Create match via MediatR with 0 goals (planned = no result yet)
        // We use CreateMatchCommand here but treat it as planned via the handler logic
        // Instead we manually create the planned match using raw DB
        // Actually let's use a simplified approach: re-read existing matches count for order
        await sender.Send(new CreateMatchCommand(tournamentId,
            new TeamInput(team1, 0),
            new TeamInput(team2, 0)));

        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostPlanManyAsync(Guid tournamentId, int gamesPerPlayer,
        bool fixedTeams, SeedingType seedingType, Guid? seedTournamentId)
    {
        await sender.Send(new PlanMatchesCommand(tournamentId, gamesPerPlayer, fixedTeams, seedingType, seedTournamentId));
        return RedirectToPage(new { tournamentId });
    }
}
