using Idasletten.Data;
using Idasletten.Shared.Domain;
using MediatR;

namespace Idasletten.Features.Matches.Queries;

public record MatchesView(IReadOnlyList<MatchView> Planned, IReadOnlyList<MatchView> Results);

public record GetMatchesQuery(Guid TournamentId) : IRequest<MatchesView>;

public class GetMatchesHandler : IRequestHandler<GetMatchesQuery, MatchesView>
{
    private readonly AppDbContext _db;
    public GetMatchesHandler(AppDbContext db) => _db = db;

    public async Task<MatchesView> Handle(GetMatchesQuery q, CancellationToken ct)
    {
        var all = await MatchProjection.LoadAsync(_db, q.TournamentId, ct);
        return new MatchesView(
            all.Where(m => m.State == MatchState.Planned).OrderBy(m => m.Order).ToList(),
            all.Where(m => m.State == MatchState.Done).OrderByDescending(m => m.Order).ToList());
    }
}

public record GetMatchQuery(Guid MatchId) : IRequest<MatchView?>;

public class GetMatchHandler : IRequestHandler<GetMatchQuery, MatchView?>
{
    private readonly AppDbContext _db;
    public GetMatchHandler(AppDbContext db) => _db = db;

    public Task<MatchView?> Handle(GetMatchQuery q, CancellationToken ct) =>
        MatchProjection.LoadOneAsync(_db, q.MatchId, ct);
}
