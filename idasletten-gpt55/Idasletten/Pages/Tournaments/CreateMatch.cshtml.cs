using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel(IMediator mediator) : PageModel
{
    public MatchEditor Editor { get; private set; } = null!;
    [BindProperty] public Guid? MatchId { get; set; }
    [BindProperty] public string Team1Initials { get; set; } = "";
    [BindProperty] public string Team2Initials { get; set; } = "";
    [BindProperty] public int Team1Goals { get; set; }
    [BindProperty] public int Team2Goals { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid? matchId)
    {
        var editor = await mediator.Send(new GetMatchEditorQuery(tournamentId, matchId));
        if (editor is null) return NotFound();
        Editor = editor;
        MatchId = editor.MatchId;
        Team1Initials = editor.Team1Initials;
        Team2Initials = editor.Team2Initials;
        Team1Goals = editor.Team1Goals;
        Team2Goals = editor.Team2Goals;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid tournamentId)
    {
        if (MatchId.HasValue)
        {
            var editor = await mediator.Send(new GetMatchEditorQuery(tournamentId, MatchId));
            if (editor?.IsDone == true && User.Identity?.IsAuthenticated != true) return Forbid();
        }
        var id = await mediator.Send(new RecordMatchCommand(tournamentId, MatchId, Split(Team1Initials), Split(Team2Initials), Team1Goals, Team2Goals));
        return RedirectToPage("/Tournaments/CreateMatch", new { tournamentId, matchId = id });
    }

    private static IReadOnlyList<string> Split(string value) => value.Split([',', ' ', '+', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
