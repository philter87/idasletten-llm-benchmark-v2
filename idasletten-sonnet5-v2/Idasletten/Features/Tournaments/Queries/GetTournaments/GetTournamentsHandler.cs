using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetTournaments;

public class GetTournamentsHandler(IdaslettenDbContext db) : IRequestHandler<GetTournamentsQuery, IReadOnlyList<TournamentSummaryDto>>
{
    public async Task<IReadOnlyList<TournamentSummaryDto>> Handle(GetTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Tournaments
            .Include(t => t.Players)
            .Where(t => t.ParentTournamentId == null);

        if (request.Scope == TournamentListScope.Public)
        {
            query = query.Where(t => t.IsPublic && !t.IsArchived);
        }

        var tournaments = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return tournaments
            .Select(t => new TournamentSummaryDto(
                t.Id,
                t.Name,
                t.TeamSize,
                t.ScoreSystem,
                t.IsArchived,
                t.IsPublic,
                t.Players.Count,
                t.RoundNumber))
            .ToList();
    }
}
