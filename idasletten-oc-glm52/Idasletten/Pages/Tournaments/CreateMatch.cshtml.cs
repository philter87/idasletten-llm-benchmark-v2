using Idasletten.Features.Matches.Commands;
using Idasletten.Features.Players.Queries;
using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Pages.Tournaments;

public class CreateMatchModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IdaslettenDbContext _db;
    public CreateMatchModel(IMediator mediator, IdaslettenDbContext db) { _mediator = mediator; _db = db; }

    public TournamentView Tournament { get; private set; } = null!;
    public List<List<string>> TeamsInitials { get; set; } = new() { new() };
    public List<int> Scores { get; set; } = new() { 0, 0 };
    public Guid? MatchId { get; set; }
    public bool ReadOnly { get; set; }
    public List<PlayerView> AvailablePlayers { get; private set; } = new();

    public async Task<IActionResult> OnGet(Guid id, Guid? matchId, bool? view)
    {
        var t = await _mediator.Send(new GetTournamentQuery(id));
        if (t is null) return NotFound();
        Tournament = t;
        AvailablePlayers = await _mediator.Send(new ListTournamentPlayersQuery(id));
        MatchId = matchId;
        ReadOnly = view == true;

        TeamsInitials = new();
        for (int i = 0; i < 2; i++) TeamsInitials.Add(new());
        Scores = new() { 0, t.PointsToWin };

        if (matchId.HasValue)
        {
            var match = await _db.TournamentMatches
                .Include(m => m.Teams!).ThenInclude(team => team.Players).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == matchId);
            if (match != null && match.Teams?.Count >= 2)
            {
                TeamsInitials = match.Teams.OrderBy(team => team.Number).Select(team =>
                    team.Players.Select(p => p.User.Username).ToList()).ToList();
                var results = await _db.TournamentTeamMatchResults.Where(r => r.MatchId == match.Id).ToListAsync();
                if (results.Count == 2)
                {
                    var ordered = match.Teams.OrderBy(team => team.Number).Select(team => team.Id).ToList();
                    Scores = ordered.Select(tid => results.First(r => r.TeamId == tid).GoalsWon).ToList();
                }
            }
        }
        return Page();
    }

    public async Task<IActionResult> OnPost(Guid tournamentId, List<List<string>> teamsInitials, List<int> scores, Guid? matchId, string? returnUrl)
    {
        var id = await _mediator.Send(new CreateMatchCommand(tournamentId, matchId, teamsInitials, scores));
        return LocalRedirect(returnUrl ?? Url.Page("/Tournaments/Detail", new { id = tournamentId })!);
    }
}