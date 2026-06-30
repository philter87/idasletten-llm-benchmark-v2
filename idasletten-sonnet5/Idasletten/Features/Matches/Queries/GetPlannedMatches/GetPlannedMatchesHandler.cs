using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries.GetPlannedMatches;

public class GetPlannedMatchesHandler(IdaslettenDbContext db)
    : IRequestHandler<GetPlannedMatchesQuery, IReadOnlyList<MatchSummaryDto>>
{
    public async Task<IReadOnlyList<MatchSummaryDto>> Handle(GetPlannedMatchesQuery request, CancellationToken cancellationToken)
    {
        var matches = await db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId && m.State == MatchState.Planned)
            .OrderBy(m => m.Order)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return await MatchSummaryBuilder.BuildAsync(db, matches, cancellationToken);
    }
}
