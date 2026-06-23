using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idasletten.Pages.Tournaments;

public class DetailModel : PageModel
{
    private readonly IMediator _mediator;

    public DetailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentMatch> PlannedMatches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentMatch> RecentMatches { get; set; } = new List<TournamentMatch>();

    public async Task<IActionResult> OnGetAsync(Guid tournamentId)
    {
        var result = await _mediator.Send(new GetTournamentDetailQuery(tournamentId));
        
        if (result.Tournament == null)
        {
            return NotFound();
        }
        
        Tournament = result.Tournament;
        Players = result.Players;
        Matches = result.Matches;
        PlannedMatches = result.PlannedMatches;
        RecentMatches = result.RecentMatches;
        
        return Page();
    }
}

public class GetTournamentDetailQuery : IRequest<TournamentDetailResult>
{
    public Guid TournamentId { get; }
    
    public GetTournamentDetailQuery(Guid tournamentId)
    {
        TournamentId = tournamentId;
    }
}

public class TournamentDetailResult
{
    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentMatch> PlannedMatches { get; set; } = new List<TournamentMatch>();
    public ICollection<TournamentMatch> RecentMatches { get; set; } = new List<TournamentMatch>();
}

public class GetTournamentDetailHandler : IRequestHandler<GetTournamentDetailQuery, TournamentDetailResult>
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;

    public GetTournamentDetailHandler(AppDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<TournamentDetailResult> Handle(GetTournamentDetailQuery request, CancellationToken cancellationToken)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.Players)
                .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Teams)
                    .ThenInclude(tt => tt.Players)
                        .ThenInclude(tp => tp.User)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Results)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);

        if (tournament == null)
        {
            return new TournamentDetailResult { Tournament = null! };
        }

        var players = tournament.Players.ToList();
        var matches = tournament.Matches.ToList();
        var plannedMatches = matches.Where(m => m.State == MatchState.Planned).ToList();
        var recentMatches = matches.Where(m => m.State == MatchState.Done).OrderByDescending(m => m.Order).Take(5).ToList();

        // Publish event
        await _publisher.Publish(new TournamentViewed(request.TournamentId), cancellationToken);

        return new TournamentDetailResult
        {
            Tournament = tournament,
            Players = players,
            Matches = matches,
            PlannedMatches = plannedMatches,
            RecentMatches = recentMatches
        };
    }
}

public record TournamentViewed(Guid TournamentId) : INotification;
