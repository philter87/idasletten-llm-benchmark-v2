using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Matches.Queries.GetAllMatches;

public class GetAllMatchesHandler(IdaslettenDbContext db) : IRequestHandler<GetAllMatchesQuery, AllMatchesDto>
{
    public async Task<AllMatchesDto> Handle(GetAllMatchesQuery request, CancellationToken cancellationToken)
    {
        var planned = await db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId && m.State == MatchState.Planned)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);

        var completed = await db.TournamentMatches
            .Where(m => m.TournamentId == request.TournamentId && m.State == MatchState.Done)
            .OrderByDescending(m => m.Order)
            .ToListAsync(cancellationToken);

        var plannedDtos = await MatchSummaryBuilder.BuildAsync(db, planned, cancellationToken);
        var completedDtos = await MatchSummaryBuilder.BuildAsync(db, completed, cancellationToken);

        return new AllMatchesDto(plannedDtos, completedDtos);
    }
}
