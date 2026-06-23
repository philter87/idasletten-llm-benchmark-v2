using Idasletten.Features.Matches.Commands.CreateMatch;
using Idasletten.Features.Players.Queries.GetTournamentPlayers;
using Idasletten.Features.Tournaments.Queries.GetTournament;
using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchPageModel(ISender sender, AppDbContext db) : PageModel
{
    public Tournament? Tournament { get; set; }
    public List<TournamentPlayer> ExistingPlayers { get; set; } = [];
    public Guid? ExistingMatchId { get; set; }

    [BindProperty] public List<string> Team1Players { get; set; } = [];
    [BindProperty] public List<string> Team2Players { get; set; } = [];
    [BindProperty] public int Team1Goals { get; set; }
    [BindProperty] public int Team2Goals { get; set; }
    [BindProperty] public Guid TournamentId { get; set; }
    [BindProperty] public Guid? ExistingMatchIdInput { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid tournamentId, [FromQuery] Guid? matchId)
    {
        Tournament = await sender.Send(new GetTournamentQuery(tournamentId));
        if (Tournament is null) return NotFound();

        ExistingPlayers = await sender.Send(new GetTournamentPlayersQuery(tournamentId));
        ExistingMatchId = matchId;

        if (matchId.HasValue)
        {
            var match = await db.TournamentMatches
                .Include(m => m.TeamResults).ThenInclude(r => r.Team).ThenInclude(t => t.Players).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == matchId.Value);

            if (match is not null && match.TeamResults.Count >= 2)
            {
                var teams = match.TeamResults.ToList();
                Team1Players = teams[0].Team.Players.Select(p => p.User.Username).ToList();
                Team2Players = teams[1].Team.Players.Select(p => p.User.Username).ToList();
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Tournament = await sender.Send(new GetTournamentQuery(TournamentId));
        if (Tournament is null) return NotFound();

        ExistingPlayers = await sender.Send(new GetTournamentPlayersQuery(TournamentId));

        if (!ModelState.IsValid) return Page();

        var matchId = await sender.Send(new CreateMatchCommand(
            TournamentId,
            new TeamInput(Team1Players, Team1Goals),
            new TeamInput(Team2Players, Team2Goals),
            ExistingMatchIdInput
        ));

        return RedirectToPage("/Tournaments/Detail", new { tournamentId = TournamentId });
    }
}
