using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailModel(IMediator mediator) => _mediator = mediator;

    public Tournament? Tournament { get; set; }
    public List<TournamentMatch> PlannedMatches { get; set; } = [];
    public List<TournamentMatch> RecentMatches { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        Tournament = await _mediator.Send(new GetTournamentByIdQuery(tournamentId));
        if (Tournament == null) return NotFound();

        var matches = await _mediator.Send(new GetMatchesForTournamentQuery(tournamentId));
        PlannedMatches = matches.Planned.Take(5).ToList();
        RecentMatches = matches.Completed.Take(5).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayerAsync(Guid tournamentId, string username, string? name)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            ModelState.AddModelError("", "Username is required");
            return await OnGetAsync(tournamentId);
        }

        await _mediator.Send(new AddPlayerToTournamentCommand(username, name, tournamentId));
        return RedirectToPage(new { tournamentId });
    }
}
