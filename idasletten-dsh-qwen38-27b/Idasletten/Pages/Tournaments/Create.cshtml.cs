using System.ComponentModel.DataAnnotations;
using Idasletten.Auth;
using Idasletten.Features.Common;
using Idasletten.Features.Players.Queries.GetSourceTournamentPlayers;
using Idasletten.Features.Tournaments.Commands.CreateTournament;
using Idasletten.Features.Tournaments.Queries.GetAllTournaments;
using Idasletten.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

[Authorize(Policy = AuthConstants.IdentityPolicy)]
public class CreateModel : PageModel
{
    private readonly IMediator _mediator;

    public CreateModel(IMediator mediator) => _mediator = mediator;

    [BindProperty(SupportsGet = true)]
    public string? Name { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MaxPlayerCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public int TeamSize { get; set; } = 2;

    [BindProperty(SupportsGet = true)]
    public int PointsToWin { get; set; } = 5;

    [BindProperty(SupportsGet = true)]
    public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

    [BindProperty(SupportsGet = true)]
    public bool IsPublic { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public Guid? ParentTournamentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PlanAfter { get; set; }

    public List<Features.Tournaments.TournamentCardDto> AllTournaments { get; set; } = new();
    public List<Features.Players.Queries.GetSelectablePlayers.PlayerSelectDto> ParentPlayers { get; set; } = new();
    public string ScoreSystemInfo { get; set; } = "";

    public Dictionary<string, string> ScoreSystemInfoAll { get; } = new()
    {
        ["Elo"] = "Standard Elo (base 1500, K=32). Multi-player teams use the average of their players' ratings.",
        ["TrueSkill"] = "Bayesian TrueSkill (mu/sigma) — the moserware/Skills engine. Wins raise your mu, losses lower it.",
        ["Lives"] = "Everyone starts with 3 lives. Lose a match and you lose a life; at 0 lives you are eliminated.",
        ["WinCount"] = "Score = number of wins. Ties are broken by goal difference, then fewer goals lost.",
    };

    public string ScoreSystemInfoJson => System.Text.Json.JsonSerializer.Serialize(ScoreSystemInfoAll);

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> ScoreSystemOptions { get; } = new()
    {
        new("Elo", "Elo"),
        new("TrueSkill", "TrueSkill"),
        new("Lives", "Lives"),
        new("WinCount", "WinCount")
    };

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> VisibilityOptions { get; } = new()
    {
        new("Public", "True"),
        new("Private", "False")
    };

    public async Task OnGetAsync()
    {
        AllTournaments = (await _mediator.Send(new GetAllTournamentsQuery(true))).ToList();
        await LoadParentPlayersAsync();
        UpdateInfo();
    }

    public async Task OnPostAsync()
    {
        try
        {
            var carryOver = Request.Form["carryOver"].ToList();
            var userIds = new List<Guid>();
            foreach (var id in carryOver)
                if (Guid.TryParse(id, out var g)) userIds.Add(g);

            var tournamentId = await _mediator.Send(new CreateTournamentCommand(
                Name, MaxPlayerCount, TeamSize, PointsToWin, ScoreSystem, IsPublic,
                ParentTournamentId, userIds));

            var planAfter = !string.IsNullOrEmpty(PlanAfter);
            TempData["Success"] = planAfter
                ? "Tournament created — plan the matches now."
                : "Tournament created.";

            Response.Redirect(planAfter ? $"/tournaments/{tournamentId}/matches" : $"/tournaments/{tournamentId}");
            return;
        }
        catch (FeatureException ex)
        {
            TempData["Error"] = ex.Message;
            AllTournaments = (await _mediator.Send(new GetAllTournamentsQuery(true))).ToList();
            await LoadParentPlayersAsync();
            UpdateInfo();
        }
    }

    private async Task LoadParentPlayersAsync()
    {
        ParentPlayers = new List<Features.Players.Queries.GetSelectablePlayers.PlayerSelectDto>();
        if (ParentTournamentId is Guid pid)
        {
            var players = await _mediator.Send(new GetSourceTournamentPlayersQuery(pid, Guid.Empty));
            ParentPlayers = players?.ToList() ?? new();
        }
    }

    private void UpdateInfo() => ScoreSystemInfo = ScoreSystemInfoAll[ScoreSystem.ToString()];
}
