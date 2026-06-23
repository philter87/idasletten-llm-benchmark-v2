using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Matches.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class MatchesModel : PageModel
{
    private readonly IMediator _mediator;
    public MatchesModel(IMediator mediator) => _mediator = mediator;

    public TournamentDetail Tournament { get; private set; } = null!;
    public MatchesView Matches { get; private set; } = null!;
    public IReadOnlyList<TournamentListItem> SeedCandidates { get; private set; } = new List<TournamentListItem>();

    [BindProperty] public string? PlannedTeamA { get; set; }
    [BindProperty] public string? PlannedTeamB { get; set; }

    [BindProperty] public int GamesPerPlayer { get; set; } = 3;
    [BindProperty] public bool FixedTeam { get; set; }
    [BindProperty] public SeedingType Seeding { get; set; } = SeedingType.Random;
    [BindProperty] public Guid? SeedTournamentId { get; set; }

    public async Task<IActionResult> OnGet(Guid tournamentId)
    {
        if (!await LoadAsync(tournamentId)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAddPlanned(Guid tournamentId)
    {
        var teams = new List<TeamInput>
        {
            new(Split(PlannedTeamA), null),
            new(Split(PlannedTeamB), null)
        };
        if (teams.Any(t => t.Initials.Count > 0))
            await _mediator.Send(new CreateOrUpdateMatchCommand(tournamentId, null, teams));
        return RedirectToPage(new { tournamentId });
    }

    public async Task<IActionResult> OnPostPlanSeveral(Guid tournamentId)
    {
        await _mediator.Send(new PlanMatchesCommand(tournamentId, GamesPerPlayer, FixedTeam, Seeding, SeedTournamentId));
        return RedirectToPage(new { tournamentId });
    }

    private async Task<bool> LoadAsync(Guid tournamentId)
    {
        var detail = await _mediator.Send(new GetTournamentDetailQuery(tournamentId));
        if (detail is null) return false;
        Tournament = detail;
        Matches = await _mediator.Send(new GetMatchesQuery(tournamentId));
        SeedCandidates = (await _mediator.Send(new ListTournamentsQuery(IncludeChildren: true)))
            .Where(t => t.Id != tournamentId).ToList();
        SeedTournamentId = detail.SeedTournamentId;
        return true;
    }

    private static List<string> Split(string? csv) =>
        (csv ?? "").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
