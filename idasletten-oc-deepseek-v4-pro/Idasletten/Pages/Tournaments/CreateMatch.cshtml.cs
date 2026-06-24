using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Tournaments.Queries;
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

    [BindProperty]
    public Guid? MatchId { get; set; }

    [BindProperty]
    public string Team1Player1Initials { get; set; } = "";

    [BindProperty]
    public string? Team1Player2Initials { get; set; }

    [BindProperty]
    public string Team2Player1Initials { get; set; } = "";

    [BindProperty]
    public string? Team2Player2Initials { get; set; }

    [BindProperty]
    public int Team1Goals { get; set; }

    [BindProperty]
    public int Team2Goals { get; set; }

    public bool IsEditing => MatchId.HasValue;

    public async Task OnGetAsync(Guid tournamentId, Guid? matchId = null)
    {
        TournamentId = tournamentId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _mediator.Send(new RecordMatchResultCommand(
            TournamentId,
            MatchId,
            Team1Player1Initials,
            Team1Player2Initials,
            Team2Player1Initials,
            Team2Player2Initials,
            Team1Goals,
            Team2Goals
        ));

        return RedirectToPage("Detail", new { tournamentId = TournamentId });
    }
}
