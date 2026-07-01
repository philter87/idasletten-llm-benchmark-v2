using Idasletten.Features.Matches.Commands.SaveMatchResult;
using Idasletten.Features.Matches.Queries.GetMatchDetail;
using Idasletten.Features.Players.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel(ISender sender) : PageModel
{
    public TournamentDetailResult Tournament { get; private set; } = null!;
    public IReadOnlyList<TournamentPlayerDto> ExistingPlayers { get; private set; } = [];
    public MatchDetailDto? ExistingMatch { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid MatchId { get; set; }

    [BindProperty]
    public string Team1Initials { get; set; } = string.Empty;

    [BindProperty]
    public int Team1Goals { get; set; }

    [BindProperty]
    public string Team2Initials { get; set; } = string.Empty;

    [BindProperty]
    public int Team2Goals { get; set; }

    public bool IsReadOnly => ExistingMatch is { State: MatchState.Done } && User.Identity?.IsAuthenticated != true;

    public async Task<IActionResult> OnGetAsync()
    {
        var tournament = await sender.Send(new GetTournamentDetailQuery(Id));
        if (tournament is null)
        {
            return NotFound();
        }
        Tournament = tournament;
        ExistingPlayers = await sender.Send(new GetTournamentPlayersQuery(Id));

        ExistingMatch = await sender.Send(new GetMatchDetailQuery(MatchId));
        if (ExistingMatch is not null)
        {
            var teams = ExistingMatch.Teams;
            if (teams.Count > 0)
            {
                Team1Initials = string.Join(", ", teams[0].PlayerUsernames);
                Team1Goals = teams[0].GoalsWon ?? 0;
            }
            if (teams.Count > 1)
            {
                Team2Initials = string.Join(", ", teams[1].PlayerUsernames);
                Team2Goals = teams[1].GoalsWon ?? 0;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var team1 = SplitInitials(Team1Initials);
        var team2 = SplitInitials(Team2Initials);

        if (team1.Count == 0 || team2.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Both teams need at least one player.");
            return await OnGetAsync();
        }

        try
        {
            await sender.Send(new SaveMatchResultCommand(
                MatchId,
                Id,
                [new MatchTeamInput(team1, Team1Goals), new MatchTeamInput(team2, Team2Goals)],
                User.Identity?.IsAuthenticated == true));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "You must log in to edit a completed match.";
            return RedirectToPage("/Login", new { returnUrl = Request.Path + Request.QueryString });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await OnGetAsync();
        }

        return RedirectToPage("/Tournaments/Details", new { id = Id });
    }

    private static List<string> SplitInitials(string input) => input
        .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}
