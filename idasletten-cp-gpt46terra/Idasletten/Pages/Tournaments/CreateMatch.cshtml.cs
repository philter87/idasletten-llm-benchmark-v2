using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel(ISender sender) : PageModel
{
    public TournamentDetail? Tournament { get; private set; }
    public TournamentMatchDetail? ExistingMatch { get; private set; }
    [BindProperty] public Guid? MatchId { get; set; }
    [BindProperty] public bool IsPlanned { get; set; }
    [BindProperty, Required] public string TeamOne { get; set; } = "";
    [BindProperty, Required] public string TeamTwo { get; set; } = "";
    [BindProperty, Range(0, 100)] public int TeamOneScore { get; set; }
    [BindProperty, Range(0, 100)] public int TeamTwoScore { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid? matchId, bool planned = false)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (Tournament is null) return NotFound();
        MatchId = matchId;
        IsPlanned = planned;
        if (matchId is { } id)
        {
            ExistingMatch = await sender.Send(new GetMatchQuery(tournamentId, id));
            if (ExistingMatch is null) return NotFound();
            TeamOne = string.Join(", ", ExistingMatch.FirstTeam);
            TeamTwo = string.Join(", ", ExistingMatch.SecondTeam);
            TeamOneScore = ExistingMatch.FirstScore ?? 0;
            TeamTwoScore = ExistingMatch.SecondScore ?? 0;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid tournamentId)
    {
        var existing = MatchId is { } id ? await sender.Send(new GetMatchQuery(tournamentId, id)) : null;
        if (existing?.State == MatchState.Done && !(User.Identity?.IsAuthenticated ?? false))
            return Challenge();
        if (!ModelState.IsValid)
        {
            Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
            return Page();
        }
        var teams = new[] { Split(TeamOne), Split(TeamTwo) };
        var match = await sender.Send(new SaveMatchCommand(tournamentId, MatchId, teams, [TeamOneScore, TeamTwoScore], IsPlanned));
        return RedirectToPage(IsPlanned ? "/Tournaments/Matches" : "/Tournaments/Details", new { tournamentId, match });
    }

    private static IReadOnlyList<string> Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
