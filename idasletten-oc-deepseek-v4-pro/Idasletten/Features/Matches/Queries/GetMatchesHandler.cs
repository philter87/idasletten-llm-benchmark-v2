using Idasletten.Features.Matches.Queries;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public class GetMatchesHandler :
    IRequestHandler<GetMatchesForTournamentQuery, MatchesResult>,
    IRequestHandler<GetMatchByIdQuery, MatchViewModel?>
{
    private readonly AppDbContext _db;

    public GetMatchesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MatchesResult> Handle(GetMatchesForTournamentQuery query, CancellationToken ct)
    {
        var matches = await _db.TournamentMatches
            .Include(m => m.TeamResults)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.TeamPlayers)
                        .ThenInclude(tp => tp.Player)
                            .ThenInclude(p => p.User)
            .Where(m => m.TournamentId == query.TournamentId)
            .OrderBy(m => m.Order)
            .ToListAsync(ct);

        var result = new MatchesResult();
        foreach (var m in matches)
        {
            var vm = MapMatch(m);
            if (m.State == Shared.Entities.MatchState.Planned)
                result.Planned.Add(vm);
            else
                result.Completed.Add(vm);
        }

        return result;
    }

    public async Task<MatchViewModel?> Handle(GetMatchByIdQuery query, CancellationToken ct)
    {
        var match = await _db.TournamentMatches
            .Include(m => m.TeamResults)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.TeamPlayers)
                        .ThenInclude(tp => tp.Player)
                            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(m => m.Id == query.MatchId && m.TournamentId == query.TournamentId, ct);

        return match == null ? null : MapMatch(match);
    }

    private static MatchViewModel MapMatch(Shared.Entities.TournamentMatch m)
    {
        return new MatchViewModel
        {
            Id = m.Id,
            Order = m.Order,
            State = m.State.ToString(),
            CreatedAt = m.CreatedAt,
            PlayedAt = m.PlayedAt,
            Teams = m.TeamResults.Select(r => new TeamViewModel
            {
                Id = r.TeamId,
                Name = r.Team.Name,
                Number = r.Team.Number,
                GoalsWon = r.GoalsWon,
                GoalsLost = r.GoalsLost,
                PlayerInitials = r.Team.TeamPlayers
                    .Select(tp => tp.Player.User.Username)
                    .ToList()
            }).ToList()
        };
    }
}
