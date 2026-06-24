using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Tournament? Tournament { get; set; }
    public List<TournamentMatch> UpcomingMatches { get; set; } = new();
    public List<TournamentMatch> RecentMatches { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        Tournament = await _mediator.Send(new GetTournamentByIdQuery(tournamentId));
        if (Tournament == null)
            return NotFound();

        var allMatches = await _mediator.Send(new GetMatchesQuery(tournamentId));
        UpcomingMatches = allMatches.Where(m => m.State == MatchState.Planned).Take(5).ToList();
        RecentMatches = allMatches.Where(m => m.State == MatchState.Done).OrderByDescending(m => m.Order).Take(5).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string username, string? name)
    {
        await _mediator.Send(new AddPlayerCommand(tournamentId, username, name));
        return RedirectToPage();
    }
}
