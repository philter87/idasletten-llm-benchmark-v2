using System.ComponentModel.DataAnnotations;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Tournaments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

/// <summary>Creating a tournament is the one thing that requires a login (see Program.cs).</summary>
public class CreateModel(ISender sender) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<TournamentSummary> PreviousTournaments { get; private set; } = [];

    public TournamentDetail? Parent { get; private set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Turneringen skal have et navn")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 1000, ErrorMessage = "Der skal være plads til mindst én spiller")]
        public int? MaxPlayerCount { get; set; }

        [Range(1, 10)]
        public int TeamSize { get; set; } = 2;

        [Range(1, 100)]
        public int PointsToWin { get; set; } = 5;

        public ScoreSystem ScoreSystem { get; set; } = ScoreSystem.Elo;

        public bool IsPublic { get; set; } = true;

        /// <summary>Optional: a previous tournament whose results seed the planning of this one.</summary>
        public Guid? SeedTournamentId { get; set; }

        /// <summary>Set when this tournament is the next round of another tournament.</summary>
        public Guid? ParentTournamentId { get; set; }

        [Range(2, 1000)]
        public int? AdvancingPlayerCount { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid? parentTournamentId, Guid? seedTournamentId)
    {
        await LoadAsync(parentTournamentId);

        Input.ParentTournamentId = parentTournamentId;
        Input.SeedTournamentId = seedTournamentId;

        if (Parent is not null)
        {
            // A new round inherits the rules of the tournament it continues.
            Input.Name = $"{Parent.Name} - runde {(Parent.RoundNumber ?? 1) + 1}";
            Input.TeamSize = Parent.TeamSize;
            Input.PointsToWin = Parent.PointsToWin;
            Input.ScoreSystem = Parent.ScoreSystem;
            Input.IsPublic = Parent.IsPublic;
            Input.AdvancingPlayerCount = Math.Max(2, Parent.PlayerCount / 2);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool plan)
    {
        await LoadAsync(Input.ParentTournamentId);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var tournamentId = await sender.Send(new CreateTournament(
            Input.Name,
            Input.TeamSize,
            Input.PointsToWin,
            Input.ScoreSystem,
            Input.MaxPlayerCount,
            Input.IsPublic,
            Input.SeedTournamentId,
            Input.ParentTournamentId,
            Input.AdvancingPlayerCount));

        // "Opret og planlæg" goes straight to the match page with the planning dialog open.
        return plan
            ? RedirectToPage("/Tournaments/Matches", new { tournamentId, plan = true })
            : RedirectToPage("/Tournaments/Detail", new { tournamentId });
    }

    private async Task LoadAsync(Guid? parentTournamentId)
    {
        PreviousTournaments = await sender.Send(new GetTournaments());

        Parent = parentTournamentId is { } parentId
            ? await sender.Send(new GetTournament(parentId))
            : null;
    }
}
