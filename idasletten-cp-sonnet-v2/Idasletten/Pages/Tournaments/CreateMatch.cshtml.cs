using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateMatchModel(IMediator mediator) => _mediator = mediator;

    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ExistingMatchId { get; set; }

    [BindProperty, Required]
    public string Team1Initials { get; set; } = string.Empty;

    [BindProperty, Required]
    public string Team2Initials { get; set; } = string.Empty;

    [BindProperty, Range(0, 99)]
    public int Team1Goals { get; set; }

    [BindProperty, Range(0, 99)]
    public int Team2Goals { get; set; }

    public Tournament? Tournament { get; set; }
    public TournamentMatch? ExistingMatch { get; set; }
    public List<TournamentPlayer> TournamentPlayers { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid? matchId = null)
    {
        TournamentId = tournamentId;
        ExistingMatchId = matchId;

        Tournament = await _mediator.Send(new GetTournamentByIdQuery(tournamentId));
        if (Tournament == null) return NotFound();

        TournamentPlayers = Tournament.Players.OrderBy(p => p.User.Username).ToList();

        if (matchId.HasValue)
        {
            ExistingMatch = await _mediator.Send(new GetMatchByIdQuery(matchId.Value));
            if (ExistingMatch != null)
            {
                var t1 = ExistingMatch.TeamResults.FirstOrDefault();
                var t2 = ExistingMatch.TeamResults.Skip(1).FirstOrDefault();
                if (t1 != null)
                    Team1Initials = string.Join(", ", t1.Team.TeamPlayers.Select(tp => tp.Player.User.Username));
                if (t2 != null)
                    Team2Initials = string.Join(", ", t2.Team.TeamPlayers.Select(tp => tp.Player.User.Username));
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Tournament = await _mediator.Send(new GetTournamentByIdQuery(TournamentId));
            TournamentPlayers = Tournament?.Players.OrderBy(p => p.User.Username).ToList() ?? [];
            return Page();
        }

        var team1 = ParseInitials(Team1Initials);
        var team2 = ParseInitials(Team2Initials);

        if (!team1.Any() || !team2.Any())
        {
            ModelState.AddModelError("", "Both teams must have at least one player");
            Tournament = await _mediator.Send(new GetTournamentByIdQuery(TournamentId));
            TournamentPlayers = Tournament?.Players.OrderBy(p => p.User.Username).ToList() ?? [];
            return Page();
        }

        await _mediator.Send(new RecordMatchResultCommand(
            TournamentId,
            team1,
            team2,
            Team1Goals,
            Team2Goals,
            ExistingMatchId
        ));

        return RedirectToPage("/Tournaments/Detail", new { tournamentId = TournamentId });
    }

    private static List<string> ParseInitials(string input) =>
        input.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries)
             .Select(s => s.Trim())
             .Where(s => !string.IsNullOrEmpty(s))
             .ToList();
}
