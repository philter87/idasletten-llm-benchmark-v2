using Idasletten.Features.Matches.Queries.GetMatches;
using Idasletten.Features.Players.Commands.AddPlayer;
using Idasletten.Features.Players.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class TournamentDetailModel(ISender sender) : PageModel
{
    public Tournament? Tournament { get; set; }
    public List<TournamentPlayer> Players { get; set; } = [];
    public List<TournamentMatch> PlannedMatches { get; set; } = [];
    public List<TournamentMatch> RecentMatches { get; set; } = [];

    public async Task OnGetAsync(Guid tournamentId)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (Tournament is null) return;

        Players = await sender.Send(new GetTournamentPlayersQuery(tournamentId));

        var planned = await sender.Send(new GetMatchesQuery(tournamentId, MatchState.Planned));
        PlannedMatches = planned.OrderBy(m => m.Order).ToList();

        var done = await sender.Send(new GetMatchesQuery(tournamentId, MatchState.Done));
        RecentMatches = done.OrderByDescending(m => m.PlayedAt).ToList();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string initials, string? playerName)
    {
        if (!string.IsNullOrWhiteSpace(initials))
            await sender.Send(new AddPlayerCommand(tournamentId, initials, playerName));

        return RedirectToPage(new { tournamentId });
    }
}
