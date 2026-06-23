using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Commands;
using Idasletten.Features.Users;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    [BindProperty]
    public CreateMatchCommand Command { get; set; } = new();

    public Guid TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public int TournamentTeamSize { get; set; } = 2;
    public int TournamentPointsToWin { get; set; } = 5;
    public ScoreSystem TournamentScoreSystem { get; set; } = ScoreSystem.Elo;
    public ICollection<TournamentPlayer> AvailablePlayers { get; set; } = new List<TournamentPlayer>();
    public TournamentMatch? CurrentMatch { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid tournamentId, Guid? matchId = null)
    {
        var tournament = await _mediator.Send(new GetTournamentForMatchQuery(tournamentId));
        
        if (tournament == null)
        {
            return NotFound();
        }

        TournamentId = tournamentId;
        TournamentName = tournament.Name;
        TournamentTeamSize = tournament.TeamSize;
        TournamentPointsToWin = tournament.PointsToWin;
        TournamentScoreSystem = tournament.ScoreSystem;
        AvailablePlayers = tournament.Players;

        // If editing an existing match
        if (matchId.HasValue)
        {
            CurrentMatch = await _mediator.Send(new GetMatchByIdQuery(matchId.Value));
            
            if (CurrentMatch == null || CurrentMatch.TournamentId != tournamentId)
            {
                return NotFound();
            }

            Command.MatchId = matchId.Value;

            // Pre-fill the form with existing match data
            var team1 = CurrentMatch.Teams.OrderBy(t => t.Number).FirstOrDefault();
            var team2 = CurrentMatch.Teams.OrderBy(t => t.Number).Skip(1).FirstOrDefault();

            if (team1 != null)
            {
                Command.Team1Initials = team1.Players.Select(p => p.User.UserName).ToArray();
                var result1 = CurrentMatch.Results.FirstOrDefault(r => r.TeamId == team1.Id);
                if (result1 != null)
                {
                    Command.Team1Goals = result1.GoalsWon;
                }
            }

            if (team2 != null)
            {
                Command.Team2Initials = team2.Players.Select(p => p.User.UserName).ToArray();
                var result2 = CurrentMatch.Results.FirstOrDefault(r => r.TeamId == team2.Id);
                if (result2 != null)
                {
                    Command.Team2Goals = result2.GoalsWon;
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Check if we're editing a completed match without permission
            if (Command.MatchId != Guid.Empty && !User.Identity.IsAuthenticated)
            {
                var currentMatch = await _mediator.Send(new GetMatchByIdQuery(Command.MatchId));
                if (currentMatch?.State == MatchState.Done && !Command.OverwriteCompletedMatch)
                {
                    ErrorMessage = "Du skal være logget ind for at redigere en afsluttet kamp.";
                    return await SetupPageData(Command.TournamentId, Command.MatchId);
                }
            }

            var matchId = await _mediator.Send(Command);
            return RedirectToPage("/Tournaments/Detail", new { tournamentId = Command.TournamentId });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return await SetupPageData(Command.TournamentId, Command.MatchId);
        }
    }

    private async Task<IActionResult> SetupPageData(Guid tournamentId, Guid? matchId = null)
    {
        var tournament = await _mediator.Send(new GetTournamentForMatchQuery(tournamentId));
        
        if (tournament == null)
        {
            return NotFound();
        }

        TournamentId = tournamentId;
        TournamentName = tournament.Name;
        TournamentTeamSize = tournament.TeamSize;
        TournamentPointsToWin = tournament.PointsToWin;
        TournamentScoreSystem = tournament.ScoreSystem;
        AvailablePlayers = tournament.Players;

        if (matchId.HasValue)
        {
            CurrentMatch = await _mediator.Send(new GetMatchByIdQuery(matchId.Value));
        }

        return Page();
    }
}

public class GetTournamentForMatchQuery : IRequest<Tournament?>
{
    public Guid TournamentId { get; }
    
    public GetTournamentForMatchQuery(Guid tournamentId)
    {
        TournamentId = tournamentId;
    }
}

public class GetTournamentForMatchHandler : IRequestHandler<GetTournamentForMatchQuery, Tournament?>
{
    private readonly AppDbContext _context;

    public GetTournamentForMatchHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tournament?> Handle(GetTournamentForMatchQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tournaments
            .Include(t => t.Players)
                .ThenInclude(tp => tp.User)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
    }
}

public class GetMatchByIdQuery : IRequest<TournamentMatch?>
{
    public Guid MatchId { get; }
    
    public GetMatchByIdQuery(Guid matchId)
    {
        MatchId = matchId;
    }
}

public class GetMatchByIdHandler : IRequestHandler<GetMatchByIdQuery, TournamentMatch?>
{
    private readonly AppDbContext _context;

    public GetMatchByIdHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TournamentMatch?> Handle(GetMatchByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.TournamentMatches
            .Include(m => m.Teams)
                .ThenInclude(tt => tt.Players)
                    .ThenInclude(tp => tp.User)
            .Include(m => m.Results)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);
    }
}
