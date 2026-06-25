using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Models;
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

    [BindProperty]
    public Guid TournamentId { get; set; }

    [BindProperty]
    public Guid? MatchId { get; set; }

    public bool IsEditing => MatchId.HasValue;
    public int TeamCount { get; set; } = 2;
    public int TeamSize { get; set; } = 2;
    public List<TournamentPlayer> TournamentPlayers { get; set; } = new();

    private readonly Dictionary<string, string[]> _playerInitials = new();
    private readonly Dictionary<string, int> _goals = new();

    public string GetPlayerInitial(int team, int player)
    {
        var key = $"{team}_{player}";
        return _playerInitials.TryGetValue(key, out var initials) && player < initials.Length
            ? initials[player]
            : "";
    }

    public int GetGoals(int team)
    {
        return _goals.TryGetValue(team.ToString(), out var goals) ? goals : 0;
    }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid? matchId = null)
    {
        TournamentId = tournamentId;
        MatchId = matchId;

        var tournament = await _mediator.Send(new GetTournamentQuery(tournamentId));
        if (tournament == null)
            return NotFound();

        TeamSize = tournament.TeamSize;
        TournamentPlayers = tournament.Players.ToList();

        if (matchId.HasValue)
        {
            var match = await _mediator.Send(new GetMatchQuery(matchId.Value));
            if (match != null)
            {
                TeamCount = match.TeamResults.Count;
                var teamIndex = 0;
                foreach (var result in match.TeamResults)
                {
                    var playerIndex = 0;
                    foreach (var player in result.Team.Players)
                    {
                        _playerInitials[$"{teamIndex}_{playerIndex}"] = new[] { player.User.Username };
                        playerIndex++;
                    }
                    _goals[teamIndex.ToString()] = result.GoalsWon;
                    teamIndex++;
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid tournamentId, Guid? matchId,
        [FromForm] Dictionary<int, string[]> playerInitials, [FromForm] Dictionary<int, int> goals)
    {
        TournamentId = tournamentId;
        MatchId = matchId;

        var teamResults = new List<TeamResultDto>();
        foreach (var team in playerInitials.OrderBy(kvp => kvp.Key))
        {
            teamResults.Add(new TeamResultDto(
                team.Value.Where(p => !string.IsNullOrWhiteSpace(p)).ToList(),
                goals.GetValueOrDefault(team.Key, 0)
            ));
        }

        if (matchId.HasValue)
        {
            await _mediator.Send(new CompleteMatchCommand(matchId.Value, teamResults));
        }
        else
        {
            var newMatchId = await _mediator.Send(new CreateMatchCommand(tournamentId, 1, teamResults));
            await _mediator.Send(new CompleteMatchCommand(newMatchId, teamResults));
        }

        return RedirectToPage("/Tournaments/Detail", new { id = tournamentId });
    }
}
