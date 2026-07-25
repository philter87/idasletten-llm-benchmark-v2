using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

/// <summary>
/// Planned and played matches of a tournament. The counts are capped so the same query can serve both
/// the sidebar (next 5 / recent 5) and the full match page (everything).
/// </summary>
public record GetMatches(Guid TournamentId, int? PlannedLimit = null, int? PlayedLimit = null)
    : IRequest<MatchOverview>;

public record MatchOverview(IReadOnlyList<MatchRow> Planned, IReadOnlyList<MatchRow> Played);

public class GetMatchesHandler(AppDbContext db) : IRequestHandler<GetMatches, MatchOverview>
{
    public async Task<MatchOverview> Handle(GetMatches request, CancellationToken cancellationToken)
    {
        var matches = await db.TournamentMatches
            .AsNoTracking()
            .Where(m => m.TournamentId == request.TournamentId)
            .Include(m => m.Results)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.Players)
                        .ThenInclude(p => p.TournamentPlayer)
                            .ThenInclude(p => p.User)
            .ToListAsync(cancellationToken);

        var planned = matches
            .Where(m => m.State == MatchState.Planned)
            .OrderBy(m => m.Order)
            .Select(MatchRowMapper.Map);

        var played = matches
            .Where(m => m.State == MatchState.Done)
            .OrderByDescending(m => m.PlayedUtc ?? m.CreatedUtc)
            .ThenByDescending(m => m.Order)
            .Select(MatchRowMapper.Map);

        return new MatchOverview(
            request.PlannedLimit is { } plannedLimit ? planned.Take(plannedLimit).ToList() : planned.ToList(),
            request.PlayedLimit is { } playedLimit ? played.Take(playedLimit).ToList() : played.ToList());
    }
}
