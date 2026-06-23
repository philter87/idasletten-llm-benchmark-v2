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

    public TournamentDto? Tournament { get; set; }
    public bool IsEdit { get; set; }
    public Guid? ExistingMatchId { get; set; }
    public string? ErrorMessage { get; set; }

    private List<TeamInput> _teamInputs = [];

    public async Task OnGetAsync(Guid id, Guid? matchId)
    {
        Tournament = await _mediator.Send(new GetTournamentQuery(id));
        if (matchId.HasValue)
        {
            IsEdit = true;
            ExistingMatchId = matchId;
        }
    }

    public string GetTeamInitials(int index) =>
        index < _teamInputs.Count ? string.Join(", ", _teamInputs[index].PlayerInitials) : "";

    public int GetTeamGoals(int index) =>
        index < _teamInputs.Count ? _teamInputs[index].Goals : 0;

    public async Task<IActionResult> OnPostAsync(
        Guid tournamentId,
        List<TeamFormInput> teams,
        Guid? existingMatchId)
    {
        Tournament = await _mediator.Send(new GetTournamentQuery(tournamentId));

        var teamInputs = teams.Select(t => new TeamInput(
            t.PlayerInitials.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList(),
            t.Goals
        )).ToList();

        _teamInputs = teamInputs.Select(t => t).ToList();

        if (teamInputs.Any(t => !t.PlayerInitials.Any()))
        {
            ErrorMessage = "Alle hold skal have mindst én spiller.";
            return Page();
        }

        try
        {
            await _mediator.Send(new RecordMatchResultCommand(tournamentId, teamInputs, existingMatchId));
            return RedirectToPage("/Tournaments/Details", new { id = tournamentId });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}

public class TeamFormInput
{
    public string PlayerInitials { get; set; } = string.Empty;
    public int Goals { get; set; }
}
