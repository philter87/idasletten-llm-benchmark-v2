using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel : PageModel
{
    private readonly IMediator _mediator;
    public CreateMatchModel(IMediator mediator) => _mediator = mediator;

    public TournamentDetail Tournament { get; private set; } = null!;
    public MatchState? ExistingState { get; private set; }

    /// <summary>True when the form may be submitted (new/planned matches, or a logged-in editor).</summary>
    public bool CanEdit { get; private set; }

    [BindProperty] public Guid TournamentId { get; set; }
    [BindProperty] public Guid? MatchId { get; set; }
    [BindProperty] public List<string> TeamAInitials { get; set; } = new();
    [BindProperty] public List<string> TeamBInitials { get; set; } = new();
    [BindProperty] public int? GoalsA { get; set; }
    [BindProperty] public int? GoalsB { get; set; }

    public async Task<IActionResult> OnGet(Guid tournamentId, Guid? matchId)
    {
        var detail = await _mediator.Send(new GetTournamentDetailQuery(tournamentId));
        if (detail is null) return NotFound();
        Tournament = detail;
        TournamentId = tournamentId;
        MatchId = matchId;

        // Default to a fresh match id so this page can later edit the planned match it creates.
        MatchId ??= Guid.NewGuid();

        if (matchId is { } mid)
        {
            var match = await _mediator.Send(new GetMatchQuery(mid));
            if (match is not null)
            {
                ExistingState = match.State;
                if (match.Teams.Count > 0) TeamAInitials = match.Teams[0].PlayerInitials.ToList();
                if (match.Teams.Count > 1) TeamBInitials = match.Teams[1].PlayerInitials.ToList();
                if (match.State == MatchState.Done)
                {
                    GoalsA = match.Teams.ElementAtOrDefault(0)?.Goals;
                    GoalsB = match.Teams.ElementAtOrDefault(1)?.Goals;
                }
            }
        }

        PadTeams(detail.TeamSize);
        CanEdit = ExistingState != MatchState.Done || User.Identity?.IsAuthenticated == true;
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        // Editing a completed match requires login (results must be recalculated).
        if (MatchId is { } mid)
        {
            var match = await _mediator.Send(new GetMatchQuery(mid));
            if (match?.State == MatchState.Done && User.Identity?.IsAuthenticated != true)
                return RedirectToPage("/Login", new { returnUrl = Url.Page("/Tournaments/CreateMatch", new { tournamentId = TournamentId, matchId = mid }) });
        }

        var teams = new List<TeamInput>
        {
            new(TeamAInitials.Where(i => !string.IsNullOrWhiteSpace(i)).ToList(), GoalsA),
            new(TeamBInitials.Where(i => !string.IsNullOrWhiteSpace(i)).ToList(), GoalsB)
        };

        await _mediator.Send(new CreateOrUpdateMatchCommand(TournamentId, MatchId, teams));
        return RedirectToPage("/Tournaments/Detail", new { id = TournamentId });
    }

    private void PadTeams(int teamSize)
    {
        while (TeamAInitials.Count < teamSize) TeamAInitials.Add("");
        while (TeamBInitials.Count < teamSize) TeamBInitials.Add("");
    }
}
