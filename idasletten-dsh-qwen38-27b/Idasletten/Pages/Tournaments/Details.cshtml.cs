using Idasletten.Features.Common;
using Idasletten.Features.Players.Commands.AddPlayer;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailsModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailsModel(IMediator mediator) => _mediator = mediator;

    public Features.Tournaments.TournamentDetailDto? Tournament { get; set; }

    [BindProperty]
    public string Initials { get; set; } = "";

    [BindProperty]
    public string? PlayerName { get; set; }

    public async Task OnGetAsync(Guid id)
    {
        Tournament = await _mediator.Send(new GetTournamentDetailQuery(id));
        if (Tournament is null) { NotFound(); return; }
    }

    public async Task OnPostAddPlayerAsync(Guid id)
    {
        Tournament = await _mediator.Send(new GetTournamentDetailQuery(id));
        if (Tournament is null) { NotFound(); return; }
        try
        {
            var added = await _mediator.Send(new AddPlayerCommand(id, Initials, PlayerName));
            TempData["Success"] = $"{added.Initials} joined the tournament.";
        }
        catch (FeatureException ex)
        {
            TempData["Error"] = ex.Message;
        }
        Tournament = await _mediator.Send(new GetTournamentDetailQuery(id));
        Initials = "";
        PlayerName = null;
    }
}
