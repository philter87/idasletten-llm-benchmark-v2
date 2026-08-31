using Idasletten.Auth;
using Idasletten.Features.Common;
using Idasletten.Features.Matches.Commands.RecordMatchResult;
using Idasletten.Features.Matches.Queries.GetMatchDetail;
using Idasletten.Features.Players.Queries.GetSelectablePlayers;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using Idasletten.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateMatchModel(IMediator mediator) => _mediator = mediator;

    public Features.Tournaments.TournamentDetailDto? Tournament { get; set; }

    /// <summary>Set when viewing an existing match (planned → pre-filled, done → read-only unless editing).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? MatchId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Edit { get; set; }

    public MatchState? MatchState { get; set; }

    [BindProperty]
    public List<TeamForm> Teams { get; set; } = new();

    public List<PlayerSelectDto> SelectablePlayers { get; set; } = new();

    public class TeamForm
    {
        public List<string> PlayerInitials { get; set; } = new();
        public int Goals { get; set; }
    }

    public async Task OnGetAsync(Guid id, Guid? match, bool? edit)
    {
        Tournament = await _mediator.Send(new GetTournamentDetailQuery(id));
        if (Tournament is null) { NotFound(); return; }
        MatchId = match;
        Edit = edit == true;
        SelectablePlayers = (await _mediator.Send(new GetSelectablePlayersQuery(id))).ToList();

        if (match is Guid mid)
        {
            var m = await _mediator.Send(new GetMatchDetailQuery(mid));
            if (m is null) { NotFound(); return; }
            MatchState = m.State;
            PreFillTeams(m);
        }
        else
        {
            BuildEmptyTeams();
        }
    }

    public async Task OnPostAsync(Guid id)
    {
        Tournament = await _mediator.Send(new GetTournamentDetailQuery(id));
        if (Tournament is null) { NotFound(); return; }

        Features.Tournaments.MatchSummaryDto? existing = null;
        if (MatchId is Guid mid)
        {
            existing = await _mediator.Send(new GetMatchDetailQuery(mid));
            if (existing is null)
            {
                TempData["Error"] = "Match not found.";
                MatchState = null;
                BuildEmptyTeams();
                return;
            }
            MatchState = existing.State;
        }

        // Editing a completed match requires login (scores must be recalculated).
        if (existing is { State: Idasletten.Models.MatchState.Done } && User.Identity?.IsAuthenticated != true)
        {
            Response.Redirect($"/login?returnUrl={Uri.EscapeDataString($"/tournaments/{id}/create-match?match={MatchId}&edit=true")}");
            return;
        }

        var teamInputs = Teams
            .Select(t => new TeamInput
            {
                PlayerInitials = t.PlayerInitials.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
                Goals = t.Goals
            })
            .ToList();
        TempData["DebugTeams"] = $"{Teams.Count}:[{string.Join(";", Teams.Select(t => $"{t.PlayerInitials.Count}g{t.Goals}"))}]";

        try
        {
            var matchId = await _mediator.Send(new RecordMatchResultCommand(id, MatchId, teamInputs));
            TempData["Success"] = MatchId is null
                ? "Match recorded — scores updated."
                : "Match updated — all scores recalculated.";
            Response.Redirect($"/tournaments/{id}/create-match?match={matchId}");
            return;
        }
        catch (FeatureException ex)
        {
            TempData["Error"] = ex.Message;
            if (existing is not null)
                PreFillTeams(existing);
            else
                BuildEmptyTeams();
        }
    }

    private void BuildEmptyTeams()
    {
        var teamSize = Tournament!.TeamSize;
        Teams = new List<TeamForm>();
        for (var i = 0; i < 2; i++)
            Teams.Add(new TeamForm { PlayerInitials = Enumerable.Repeat(string.Empty, teamSize).ToList() });
    }

    private void PreFillTeams(Features.Tournaments.MatchSummaryDto m)
    {
        var teamSize = Tournament!.TeamSize;
        Teams = new List<TeamForm>();
        for (var i = 0; i < 2; i++)
        {
            var form = new TeamForm
            {
                PlayerInitials = Enumerable.Repeat(string.Empty, teamSize).ToList(),
                Goals = m.Teams.Count > i && m.Teams[i].Goals is int g ? g : 0
            };
            if (i < m.Teams.Count)
            {
                for (var j = 0; j < Math.Min(teamSize, m.Teams[i].Players.Count); j++)
                    form.PlayerInitials[j] = m.Teams[i].Players[j].Initials;
            }
            Teams.Add(form);
        }
    }
}
