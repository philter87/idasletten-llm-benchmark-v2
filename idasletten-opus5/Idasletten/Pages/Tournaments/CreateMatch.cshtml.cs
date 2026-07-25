using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

/// <summary>
/// One page for creating, planning, editing and viewing a match. No login is needed to record a
/// result - only to change a match that has already been played.
/// The match id is generated when the page is opened, so the very same page can edit it later.
/// </summary>
public class CreateMatchModel(ISender sender) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid TournamentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? MatchId { get; set; }

    [BindProperty]
    public List<TeamInput> Teams { get; set; } = [];

    public TournamentDetail Tournament { get; private set; } = null!;

    public IReadOnlyList<ScoreboardRow> TournamentPlayers { get; private set; } = [];

    public MatchRow? Match { get; private set; }

    public string? ErrorMessage { get; private set; }

    public bool IsPlayed => Match?.State == MatchState.Done;

    public bool IsCancelled => Match?.State == MatchState.Cancelled;

    /// <summary>A played match may only be changed by somebody who is logged in.</summary>
    public bool CanEdit => !IsPlayed || User.Identity?.IsAuthenticated == true;

    public class TeamInput
    {
        public List<string> Initials { get; set; } = [];

        public int Goals { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadAsync())
        {
            return NotFound();
        }

        MatchId ??= Guid.NewGuid();
        Teams = Match is null ? EmptyTeams() : TeamsFrom(Match);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool planned)
    {
        if (!await LoadAsync())
        {
            return NotFound();
        }

        if (!CanEdit)
        {
            return Challenge();
        }

        try
        {
            var matchId = await sender.Send(new SaveMatch(
                TournamentId,
                MatchId ?? Guid.NewGuid(),
                Teams.Select(team => new MatchTeamInput(team.Initials, team.Goals)).ToList(),
                planned));

            TempData["Message"] = planned ? "Kampen er planlagt." : "Resultatet er skrevet i sagaen.";

            return planned
                ? RedirectToPage("/Tournaments/Matches", new { tournamentId = TournamentId })
                : RedirectToPage("/Tournaments/Detail", new { tournamentId = TournamentId, matchId });
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            ErrorMessage = exception.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        if (User.Identity?.IsAuthenticated != true && IsPlayed)
        {
            return Challenge();
        }

        if (MatchId is { } matchId)
        {
            await sender.Send(new CancelMatch(TournamentId, matchId));
            TempData["Message"] = "Kampen er aflyst.";
        }

        return RedirectToPage("/Tournaments/Matches", new { tournamentId = TournamentId });
    }

    private async Task<bool> LoadAsync()
    {
        var tournament = await sender.Send(new GetTournament(TournamentId));
        if (tournament is null)
        {
            return false;
        }

        Tournament = tournament;
        TournamentPlayers = await sender.Send(new GetScoreboard(TournamentId));

        Match = MatchId is { } matchId
            ? await sender.Send(new GetMatch(TournamentId, matchId))
            : null;

        return true;
    }

    private List<TeamInput> EmptyTeams() =>
    [
        new() { Initials = Enumerable.Repeat(string.Empty, Tournament.TeamSize).ToList() },
        new() { Initials = Enumerable.Repeat(string.Empty, Tournament.TeamSize).ToList() },
    ];

    private List<TeamInput> TeamsFrom(MatchRow match) =>
        match.Teams
            .Select(team => new TeamInput
            {
                Initials = team.Players
                    .Select(player => player.Initials)
                    .Concat(Enumerable.Repeat(string.Empty, Tournament.TeamSize))
                    .Take(Math.Max(Tournament.TeamSize, team.Players.Count))
                    .ToList(),
                Goals = team.Goals,
            })
            .ToList();
}
