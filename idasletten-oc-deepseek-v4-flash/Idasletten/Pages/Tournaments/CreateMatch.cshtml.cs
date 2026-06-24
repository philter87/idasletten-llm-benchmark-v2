using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Queries;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Enums;
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

    [BindProperty]
    public string Team1Player1 { get; set; } = string.Empty;
    [BindProperty]
    public string? Team1Player2 { get; set; }
    [BindProperty]
    public int Team1Score { get; set; }
    [BindProperty]
    public string Team2Player1 { get; set; } = string.Empty;
    [BindProperty]
    public string? Team2Player2 { get; set; }
    [BindProperty]
    public int Team2Score { get; set; }

    public bool IsEditing { get; set; }
    public TournamentMatch? ExistingMatch { get; set; }
    public List<TournamentPlayer> AvailablePlayers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        AvailablePlayers = await _mediator.Send(new GetPlayersQuery(TournamentId));

        if (MatchId.HasValue)
        {
            var match = await _mediator.Send(new GetMatchByIdQuery(MatchId.Value));
            if (match != null)
            {
                ExistingMatch = match;
                IsEditing = match.State == MatchState.Done;

                var teams = match.TeamEntries.Select(te => te.Team).ToList();
                if (teams.Count >= 1)
                {
                    var players1 = teams[0].PlayerEntries.Select(pe => pe.Player.User).ToList();
                    Team1Player1 = players1.ElementAtOrDefault(0)?.Initials ?? "";
                    Team1Player2 = players1.ElementAtOrDefault(1)?.Initials ?? "";
                    var result1 = match.Results.FirstOrDefault(r => r.TeamId == teams[0].Id);
                    Team1Score = result1?.GoalsWon ?? 0;
                }
                if (teams.Count >= 2)
                {
                    var players2 = teams[1].PlayerEntries.Select(pe => pe.Player.User).ToList();
                    Team2Player1 = players2.ElementAtOrDefault(0)?.Initials ?? "";
                    Team2Player2 = players2.ElementAtOrDefault(1)?.Initials ?? "";
                    var result2 = match.Results.FirstOrDefault(r => r.TeamId == teams[1].Id);
                    Team2Score = result2?.GoalsWon ?? 0;
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var team1Players = new List<string> { Team1Player1 };
        if (!string.IsNullOrEmpty(Team1Player2)) team1Players.Add(Team1Player2);
        var team2Players = new List<string> { Team2Player1 };
        if (!string.IsNullOrEmpty(Team2Player2)) team2Players.Add(Team2Player2);

        if (MatchId.HasValue)
        {
            await _mediator.Send(new RecordMatchResultCommand(MatchId.Value, Team1Score, Team2Score));
        }
        else
        {
            var matchId = Guid.NewGuid();
            await _mediator.Send(new CreateMatchCommand(TournamentId,
                new List<List<string>> { team1Players, team2Players },
                Team1Score, Team2Score));
        }

        return RedirectToPage("/Tournaments/Detail", new { tournamentId = TournamentId });
    }
}
