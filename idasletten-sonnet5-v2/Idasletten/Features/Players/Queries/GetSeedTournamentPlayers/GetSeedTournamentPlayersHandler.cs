using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries.GetSeedTournamentPlayers;

public class GetSeedTournamentPlayersHandler(IdaslettenDbContext db)
    : IRequestHandler<GetSeedTournamentPlayersQuery, IReadOnlyList<SeedTournamentPlayerDto>>
{
    public async Task<IReadOnlyList<SeedTournamentPlayerDto>> Handle(GetSeedTournamentPlayersQuery request, CancellationToken cancellationToken)
    {
        var targetUserIds = await db.TournamentPlayers
            .Where(p => p.TournamentId == request.TargetTournamentId)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        var targetUserIdSet = targetUserIds.ToHashSet();

        return await db.TournamentPlayers
            .Where(p => p.TournamentId == request.SeedTournamentId)
            .OrderByDescending(p => p.Score)
            .Select(p => new SeedTournamentPlayerDto(
                p.UserId,
                p.User.UserName ?? string.Empty,
                p.User.Name,
                p.Score,
                targetUserIdSet.Contains(p.UserId)))
            .ToListAsync(cancellationToken);
    }
}
