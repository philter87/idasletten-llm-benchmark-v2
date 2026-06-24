using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries;

public record GetMatchesForTournamentQuery(Guid TournamentId) : IRequest<MatchesResult>;

public record MatchesResult(
    List<TournamentMatch> Planned,
    List<TournamentMatch> Completed
);

public class GetMatchesForTournamentHandler : IRequestHandler<GetMatchesForTournamentQuery, MatchesResult>
{
    private readonly AppDbContext _db;

    public GetMatchesForTournamentHandler(AppDbContext db) => _db = db;

    public async Task<MatchesResult> Handle(GetMatchesForTournamentQuery request, CancellationToken ct)
    {
        var matches = await _db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId)
            .Include(m => m.TeamResults)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.TeamPlayers)
                        .ThenInclude(tp => tp.Player)
                            .ThenInclude(p => p.User)
            .OrderBy(m => m.Order)
            .ToListAsync(ct);

        return new MatchesResult(
            matches.Where(m => m.State == MatchState.Planned).ToList(),
            matches.Where(m => m.State == MatchState.Done).OrderByDescending(m => m.PlayedAt).ToList()
        );
    }
}

public record GetMatchByIdQuery(Guid MatchId) : IRequest<TournamentMatch?>;

public class GetMatchByIdHandler : IRequestHandler<GetMatchByIdQuery, TournamentMatch?>
{
    private readonly AppDbContext _db;

    public GetMatchByIdHandler(AppDbContext db) => _db = db;

    public async Task<TournamentMatch?> Handle(GetMatchByIdQuery request, CancellationToken ct)
    {
        return await _db.TournamentMatches
            .Include(m => m.TeamResults)
                .ThenInclude(r => r.Team)
                    .ThenInclude(t => t.TeamPlayers)
                        .ThenInclude(tp => tp.Player)
                            .ThenInclude(p => p.User)
            .Include(m => m.Tournament)
            .FirstOrDefaultAsync(m => m.Id == request.MatchId, ct);
    }
}
