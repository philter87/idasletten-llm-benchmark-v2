using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateMatchModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? MatchId { get; set; }

    public MatchDetailDto? Match { get; set; }
    public Guid NewMatchId { get; set; }

    [BindProperty]
    public List<TeamInput> Teams { get; set; } = [];

    public class TeamInput
    {
        public string Initials { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (MatchId.HasValue)
        {
            Match = await _mediator.Send(new GetMatchQuery(MatchId.Value), cancellationToken);
            if (Match == null) return NotFound();
            TournamentId = Match.TournamentId;

            if (Match.State == MatchState.Done && User.Identity?.IsAuthenticated != true)
            {
                return Challenge();
            }

            Teams = Match.Teams.Select(t => new TeamInput
            {
                Initials = string.Join(", ", t.Members),
                Score = t.GoalsWon
            }).ToList();
        }
        else
        {
            NewMatchId = Guid.NewGuid();
            Teams = new List<TeamInput>
            {
                new(),
                new()
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var matchId = MatchId ?? Guid.NewGuid();

        if (MatchId.HasValue)
        {
            var existing = await _mediator.Send(new GetMatchQuery(MatchId.Value), cancellationToken);
            if (existing == null) return NotFound();
            if (existing.State == MatchState.Done && !User.Identity!.IsAuthenticated)
            {
                return Forbid();
            }
        }

        var teams = Teams.Select((t, i) => new TeamResultInput(
            null,
            i + 1,
            t.Initials.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpperInvariant()).Where(s => s.Length > 0).ToList(),
            t.Score)).ToList();

        await _mediator.Send(new CreateOrUpdateMatchResultCommand(TournamentId, matchId, teams), cancellationToken);
        return RedirectToPage("/Tournaments/Detail", new { tournamentId = TournamentId });
    }
}
