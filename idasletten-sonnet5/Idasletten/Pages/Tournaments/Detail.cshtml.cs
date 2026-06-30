using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands.CreatePlannedMatch;
using Idasletten.Features.Matches.Queries.GetPlannedMatches;
using Idasletten.Features.Matches.Queries.GetRecentMatches;
using Idasletten.Features.TournamentPlayers.Commands.AddPlayerToTournament;
using Idasletten.Features.TournamentPlayers.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel(ISender sender) : PageModel
{
    public TournamentDto Tournament { get; private set; } = null!;
    public IReadOnlyList<TournamentPlayerDto> Players { get; private set; } = [];
    public IReadOnlyList<MatchSummaryDto> PlannedMatches { get; private set; } = [];
    public IReadOnlyList<MatchSummaryDto> RecentMatches { get; private set; } = [];

    [BindProperty]
    public string PlayerUsername { get; set; } = string.Empty;

    [BindProperty]
    public string? PlayerName { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        var tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (tournament is null) return NotFound();

        Tournament = tournament;
        Players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        PlannedMatches = await sender.Send(new GetPlannedMatchesQuery(tournamentId));
        RecentMatches = await sender.Send(new GetRecentMatchesQuery(tournamentId));
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId)
    {
        if (!string.IsNullOrWhiteSpace(PlayerUsername))
        {
            await sender.Send(new AddPlayerToTournamentCommand(tournamentId, PlayerUsername, PlayerName));
        }
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostCreateMatchAsync(Guid tournamentId)
    {
        var matchId = await sender.Send(new CreatePlannedMatchCommand(tournamentId));
        return Redirect($"/tournaments/{tournamentId}/create-match/{matchId}");
    }
}
