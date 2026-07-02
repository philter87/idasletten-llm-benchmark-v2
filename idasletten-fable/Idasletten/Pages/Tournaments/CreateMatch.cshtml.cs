using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel(IMediator mediator) : PageModel
{
    public Tournament Tournament { get; private set; } = null!;
    public List<TournamentPlayer> ExistingPlayers { get; private set; } = [];
    public TournamentMatch? ExistingMatch { get; private set; }

    /// <summary>A completed match is read-only until the user logs in.</summary>
    public bool IsReadOnly => ExistingMatch?.State == MatchState.Done && User.Identity?.IsAuthenticated != true;
    public bool IsEditOfDoneMatch => ExistingMatch?.State == MatchState.Done && User.Identity?.IsAuthenticated == true;

    [BindProperty(SupportsGet = true)]
    public Guid? MatchId { get; set; }

    [BindProperty]
    public List<TeamFormModel> Teams { get; set; } = [];

    public class TeamFormModel
    {
        public List<string?> Initials { get; set; } = [];
        public int Goals { get; set; }
    }

    public async Task<IActionResult> OnGet(Guid tournamentId)
    {
        if (!await Load(tournamentId))
            return NotFound();

        Teams = [new TeamFormModel(), new TeamFormModel()];
        if (ExistingMatch is not null)
        {
            Teams = ExistingMatch.Results
                .OrderBy(r => r.Team.Number)
                .Select(r => new TeamFormModel
                {
                    Initials = r.Team.Players.Select(p => (string?)p.User.UserName).ToList(),
                    Goals = r.GoalsWon
                })
                .ToList();
        }

        foreach (var team in Teams)
            while (team.Initials.Count < Tournament.TeamSize)
                team.Initials.Add(null);

        return Page();
    }

    public async Task<IActionResult> OnPost(Guid tournamentId)
    {
        if (!await Load(tournamentId))
            return NotFound();

        if (IsReadOnly)
            return Redirect($"/login?returnUrl=/tournaments/{tournamentId}/create-match?matchId={MatchId}");

        var teams = Teams
            .Select(t => new TeamResultInput(
                t.Initials.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i!.Trim()).ToList(),
                t.Goals))
            .Where(t => t.Initials.Count > 0)
            .ToList();

        try
        {
            await mediator.Send(new RecordMatchResultCommand(tournamentId, teams, MatchId));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return Page();
        }

        return Redirect($"/tournaments/{tournamentId}");
    }

    private async Task<bool> Load(Guid tournamentId)
    {
        var players = await mediator.Send(new GetTournamentPlayersQuery(tournamentId));
        if (players is null)
            return false;

        Tournament = players.Tournament;
        ExistingPlayers = players.Players;

        if (MatchId is Guid matchId)
            ExistingMatch = await mediator.Send(new GetMatchQuery(tournamentId, matchId));
        return true;
    }
}
