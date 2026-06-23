using Idasletten.Features.Players.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;
    public DetailModel(IMediator mediator) => _mediator = mediator;

    public TournamentDetail Tournament { get; private set; } = null!;

    [BindProperty] public string? Initials { get; set; }
    [BindProperty] public string? PlayerName { get; set; }

    public async Task<IActionResult> OnGet(Guid id)
    {
        var detail = await _mediator.Send(new GetTournamentDetailQuery(id));
        if (detail is null) return NotFound();
        Tournament = detail;
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlayer(Guid id)
    {
        if (!string.IsNullOrWhiteSpace(Initials))
            await _mediator.Send(new AddPlayerCommand(id, Initials!.Trim(), PlayerName));
        return RedirectToPage(new { id });
    }
}
