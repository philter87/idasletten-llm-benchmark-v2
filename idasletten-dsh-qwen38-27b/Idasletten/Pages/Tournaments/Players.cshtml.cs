using Idasletten.Features.Common;
using Idasletten.Features.Players.Commands.AddPlayer;
using Idasletten.Features.Players.Commands.AddPlayerByUser;
using Idasletten.Features.Players.Commands.RemovePlayer;
using Idasletten.Features.Players.Queries.GetSourceTournamentPlayers;
using Idasletten.Features.Players.Queries.GetSelectablePlayers;
using Idasletten.Features.Tournaments.Queries.GetPreviousTournaments;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class PlayersModel : PageModel
{
    private readonly IMediator _mediator;

    public PlayersModel(IMediator mediator) => _mediator = mediator;

    public Features.Tournaments.TournamentDetailDto? Tournament { get; set; }
    public Guid? SourceTournamentId { get; set; }
    public string? SourceTournamentName { get; set; }
    public List<PlayerSelectDto> SourcePlayers { get; set; } = new();
    public List<Features.Tournaments.TournamentCardDto> PreviousTournaments { get; set; } = new();

    [BindProperty]
    public string Initials { get; set; } = "";
    [BindProperty]
    public string? PlayerName { get; set; }

    public async Task OnGetAsync(Guid id, Guid? source)
    {
        SourceTournamentId = source;
        await LoadAsync(id);
    }

    public async Task OnPostAsync(Guid id)
    {
        var action = Request.Form["PlayerAction"].ToString();
        if (action == "add")
        {
            try
            {
                var added = await _mediator.Send(new AddPlayerCommand(id, Initials, PlayerName));
                TempData["Success"] = $"{added.Initials} joined the tournament.";
                Initials = "";
                PlayerName = null;
            }
            catch (FeatureException ex) { TempData["Error"] = ex.Message; }
        }
        else if (action == "select-source" && Guid.TryParse(Request.Form["SourceTournamentId"].ToString(), out var src))
        {
            Response.Redirect($"/tournaments/{id}/players?source={src}");
            return;
        }
        else if (action == "add-from-source" && Guid.TryParse(Request.Form["SourceUserId"].ToString(), out var uid))
        {
            try
            {
                var added = await _mediator.Send(new AddPlayerByUserCommand(id, uid));
                TempData["Success"] = $"{added.Initials} was added from the previous tournament.";
            }
            catch (FeatureException ex) { TempData["Error"] = ex.Message; }
        }
        else if (action == "remove" && Guid.TryParse(Request.Form["TournamentPlayerId"].ToString(), out var tpId))
        {
            try
            {
                await _mediator.Send(new RemovePlayerCommand(id, tpId));
                TempData["Success"] = "Player removed.";
            }
            catch (FeatureException ex) { TempData["Error"] = ex.Message; }
        }
        await LoadAsync(id);
    }

    private async Task LoadAsync(Guid id)
    {
        Tournament = await _mediator.Send(new GetTournamentDetailQuery(id));
        if (Tournament is null) { NotFound(); return; }
        PreviousTournaments = (await _mediator.Send(new GetPreviousTournamentsQuery(id))).ToList();
        SourcePlayers = new List<PlayerSelectDto>();
        if (SourceTournamentId is Guid src)
        {
            SourcePlayers = (await _mediator.Send(new GetSourceTournamentPlayersQuery(src, id)))?.ToList() ?? new();
            SourceTournamentName = PreviousTournaments.FirstOrDefault(t => t.Id == src)?.Name;
            if (SourceTournamentName is null)
                SourceTournamentId = null; // invalid source
        }
    }
}
