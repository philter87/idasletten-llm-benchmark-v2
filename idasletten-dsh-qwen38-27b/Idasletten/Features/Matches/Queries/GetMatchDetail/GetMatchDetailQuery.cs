using Idasletten.Data;
using Idasletten.Features.Tournaments;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries.GetMatchDetail;

public sealed record GetMatchDetailQuery(Guid MatchId) : IRequest<MatchSummaryDto?>;

public sealed class GetMatchDetailQueryHandler : IRequestHandler<GetMatchDetailQuery, MatchSummaryDto?>
{
    private readonly AppDbContext _db;

    public GetMatchDetailQueryHandler(AppDbContext db) => _db = db;

    public async Task<MatchSummaryDto?> Handle(GetMatchDetailQuery request, CancellationToken cancellationToken)
    {
        var match = await _db.TournamentMatches
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, cancellationToken);
        if (match is null) return null;

        var slots = await _db.MatchTeams
            .Include(ms => ms.Team)
            .Where(ms => ms.MatchId == match.Id)
            .ToListAsync(cancellationToken);
        var results = await _db.TournamentTeamMatchResults
            .Where(r => r.MatchId == match.Id)
            .ToDictionaryAsync(r => r.TeamId, cancellationToken);
        var teamIds = slots.Select(s => s.TeamId).ToList();
        var players = await _db.TeamPlayers
            .Include(tp => tp.Player)
            .ThenInclude(p => p.User)
            .Where(tp => teamIds.Contains(tp.TeamId))
            .ToListAsync(cancellationToken);

        var teams = slots
            .Select(s =>
            {
                var cells = players
                    .Where(tp => tp.TeamId == s.TeamId)
                    .OrderBy(tp => tp.Player.User.Username)
                    .Select(tp => new PlayerCellDto(tp.TournamentPlayerId, tp.Player.UserId, tp.Player.User.Username, tp.Player.User.Name))
                    .ToList();
                return new TeamSummaryDto(s.TeamId, s.Team.Name, s.Team.Number,
                    results.TryGetValue(s.TeamId, out var r) ? r.GoalsWon : null, cells);
            })
            .ToList();

        return new MatchSummaryDto(match.Id, match.Order, match.State, teams);
    }
}
