using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries.GetRecentMatches;

public class GetRecentMatchesHandler(IdaslettenDbContext db)
    : IRequestHandler<GetRecentMatchesQuery, IReadOnlyList<MatchSummaryDto>>
{
    public async Task<IReadOnlyList<MatchSummaryDto>> Handle(GetRecentMatchesQuery request, CancellationToken cancellationToken)
    {
        var matches = await db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId && m.State == MatchState.Done)
            .OrderByDescending(m => m.Order)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return await MatchSummaryBuilder.BuildAsync(db, matches, cancellationToken);
    }
}
