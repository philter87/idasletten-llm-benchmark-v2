using Idasletten.Data;
using Idasletten.Features.Tournaments;
using Idasletten.Features.Tournaments.Queries.GetTournamentDetail;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetTournamentMatches;

public sealed record GetTournamentMatchesQuery(Guid TournamentId) : IRequest<
    (IReadOnlyList<MatchSummaryDto> Planned, IReadOnlyList<MatchSummaryDto> Results)?>;

public sealed class GetTournamentMatchesQueryHandler : IRequestHandler<GetTournamentMatchesQuery,
    (IReadOnlyList<MatchSummaryDto> Planned, IReadOnlyList<MatchSummaryDto> Results)?>
{
    private readonly AppDbContext _db;

    public GetTournamentMatchesQueryHandler(AppDbContext db) => _db = db;

    public async Task<(IReadOnlyList<MatchSummaryDto> Planned, IReadOnlyList<MatchSummaryDto> Results)?> Handle(
        GetTournamentMatchesQuery request, CancellationToken cancellationToken)
    {
        var exists = await _db.Tournaments.AnyAsync(t => t.Id == request.TournamentId, cancellationToken);
        if (!exists) return null;

        var planned = await GetTournamentDetailQueryHandler.BuildMatchesAsync(_db, request.TournamentId, MatchState.Planned, int.MaxValue, latestFirst: false, cancellationToken);
        var results = await GetTournamentDetailQueryHandler.BuildMatchesAsync(_db, request.TournamentId, MatchState.Done, int.MaxValue, latestFirst: true, cancellationToken);
        return (planned, results);
    }
}
