using Idasletten.Features.Common;
using Idasletten.Features.Matches.Commands.CancelMatch;
using Idasletten.Features.Matches.Commands.PlanMatch;
using Idasletten.Features.Matches.Commands.PlanMatches;
using Idasletten.Features.Tournaments.Queries.GetAllTournaments;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using Idasletten.Features.Tournaments.Queries.GetTournamentMatches;
using Idasletten.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel : PageModel
{
    private readonly IMediator _mediator;

    public MatchesModel(IMediator mediator) => _mediator = mediator;

    public Features.Tournaments.TournamentDetailDto? Tournament { get; set; }
    public List<Features.Tournaments.MatchSummaryDto> Planned { get; set; } = new();
    public List<Features.Tournaments.MatchSummaryDto> Results { get; set; } = new();
    public List<Features.Tournaments.TournamentCardDto> PreviousTournaments { get; set; } = new();

    // Add planned match dialog fields.
    [BindProperty]
    public List<TeamForm> Teams { get; set; } = new();

    // Plan several matches dialog fields.
    [BindProperty]
    public Guid? SeedTournamentId { get; set; }

    [BindProperty]
    public int GamesPerPlayer { get; set; } = 1;

    [BindProperty]
    public bool FixedTeams { get; set; }

    [BindProperty]
    public SeedingType SeedingType { get; set; } = SeedingType.Random;

    public class TeamForm
    {
        public List<string> PlayerInitials { get; set; } = new();
    }

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> FixedTeamsOptions { get; } = new()
    {
        new("Teams reshuffle after each match", "False"),
        new("Fixed teams (round-robin groups)", "True")
    };

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SeedingTypeOptions { get; } = new()
    {
        new("Random — teams chosen randomly", "Random"),
        new("Equality — best with worst (1+N, 2+(N−1), …)", "Equality"),
        new("Fair — top half with bottom half (1+6, 2+7, …)", "Fair")
    };

    public async Task OnGetAsync(Guid id)
    {
        await LoadAsync(id);
    }

    public async Task OnPostAsync(Guid id)
    {
        // Which dialog posted? Distinguished by a hidden marker.
        var action = Request.Form["PlanAction"].ToString();
        if (action == "plan-one")
        {
            var teamInitials = Teams
                .Select(t => t.PlayerInitials.Where(s => !string.IsNullOrWhiteSpace(s)).ToList().AsReadOnly())
                .ToList();
            try
            {
                await _mediator.Send(new PlanMatchCommand(id, teamInitials));
                TempData["Success"] = "Planned match added.";
            }
            catch (FeatureException ex) { TempData["Error"] = ex.Message; }
        }
        else if (action == "plan-many")
        {
            try
            {
                var count = await _mediator.Send(new PlanMatchesCommand(
                    id, SeedTournamentId, true, GamesPerPlayer, FixedTeams, SeedingType));
                TempData["Success"] = $"{count} match(es) planned.";
            }
            catch (FeatureException ex) { TempData["Error"] = ex.Message; }
        }
        else if (action == "cancel" && Guid.TryParse(Request.Form["CancelMatchId"].ToString(), out var matchId))
        {
            try
            {
                await _mediator.Send(new CancelMatchCommand(matchId));
                TempData["Success"] = "Match cancelled.";
            }
            catch (FeatureException ex) { TempData["Error"] = ex.Message; }
        }
        await LoadAsync(id);
    }

    private async Task LoadAsync(Guid id)
    {
        Tournament = await _mediator.Send(new GetTournamentDetailQuery(id));
        if (Tournament is null) { NotFound(); return; }
        var matches = await _mediator.Send(new GetTournamentMatchesQuery(id));
        if (matches is not null)
        {
            Planned = matches.Value.Planned.ToList();
            Results = matches.Value.Results.ToList();
        }
        PreviousTournaments = (await _mediator.Send(new GetAllTournamentsQuery(true)))
            .Where(t => t.Id != id)
            .ToList();
        BuildEmptyTeams();
    }

    private void BuildEmptyTeams()
    {
        var teamSize = Tournament!.TeamSize;
        Teams = new List<TeamForm>();
        for (var i = 0; i < 2; i++)
            Teams.Add(new TeamForm { PlayerInitials = Enumerable.Repeat(string.Empty, teamSize).ToList() });
    }
}
