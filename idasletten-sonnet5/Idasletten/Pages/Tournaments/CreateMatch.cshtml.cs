using Idasletten.Features.Matches;
using Idasletten.Features.Matches.Commands.SaveMatch;
using Idasletten.Features.Matches.Queries.GetMatch;
using Idasletten.Features.TournamentPlayers.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel(ISender sender) : PageModel
{
    public TournamentDto Tournament { get; private set; } = null!;
    public MatchDetailDto Match { get; private set; } = null!;
    public IReadOnlyList<TournamentPlayerDto> TournamentPlayers { get; private set; } = [];
    public bool ReadOnly { get; private set; }

    [BindProperty]
    public List<TeamFormInput> Teams { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid matchId)
    {
        var tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        var match = await sender.Send(new GetMatchQuery(matchId));
        if (tournament is null || match is null || match.TournamentId != tournamentId) return NotFound();

        Tournament = tournament;
        Match = match;
        TournamentPlayers = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        ReadOnly = match.State == MatchState.Done && User.Identity?.IsAuthenticated != true;

        Teams = match.Teams.Count > 0
            ? match.Teams.Select(t => new TeamFormInput
            {
                InitialsCsv = string.Join(", ", t.PlayerUsernames),
                Score = t.GoalsWon ?? 0
            }).ToList()
            : [new TeamFormInput(), new TeamFormInput()];

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid tournamentId, Guid matchId, bool recordResult)
    {
        var match = await sender.Send(new GetMatchQuery(matchId));
        if (match is null) return NotFound();

        var isEditingDoneMatch = match.State == MatchState.Done;
        if (isEditingDoneMatch && User.Identity?.IsAuthenticated != true)
        {
            return Forbid();
        }

        var teamInputs = Teams
            .Select(t => new TeamInput(
                t.InitialsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                t.Score))
            .Where(t => t.Initials.Count > 0)
            .ToList();

        await sender.Send(new SaveMatchCommand(matchId, tournamentId, teamInputs, recordResult));

        return Redirect($"/tournaments/{tournamentId}");
    }

    public class TeamFormInput
    {
        public string InitialsCsv { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}
