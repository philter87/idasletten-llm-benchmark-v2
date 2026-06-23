using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;
    public DetailModel(IMediator mediator) => _mediator = mediator;

    public TournamentScoreboard Board { get; private set; } = null!;

    public async Task<IActionResult> OnGet(Guid id)
    {
        var board = await _mediator.Send(new GetTournamentScoreboardQuery(id));
        if (board is null) return NotFound();
        Board = board;
        return Page();
    }
}