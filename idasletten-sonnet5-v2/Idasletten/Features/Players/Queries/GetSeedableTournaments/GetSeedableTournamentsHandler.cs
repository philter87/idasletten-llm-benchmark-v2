using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries.GetSeedableTournaments;

public class GetSeedableTournamentsHandler(IdaslettenDbContext db)
    : IRequestHandler<GetSeedableTournamentsQuery, IReadOnlyList<SeedableTournamentDto>>
{
    public async Task<IReadOnlyList<SeedableTournamentDto>> Handle(GetSeedableTournamentsQuery request, CancellationToken cancellationToken)
    {
        return await db.Tournaments
            .Where(t => t.Id != request.ExcludeTournamentId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SeedableTournamentDto(t.Id, t.Name, t.Players.Count))
            .ToListAsync(cancellationToken);
    }
}
