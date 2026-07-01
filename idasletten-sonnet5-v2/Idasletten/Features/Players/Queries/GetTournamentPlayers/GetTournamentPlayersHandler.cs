using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries.GetTournamentPlayers;

public class GetTournamentPlayersHandler(IdaslettenDbContext db)
    : IRequestHandler<GetTournamentPlayersQuery, IReadOnlyList<TournamentPlayerDto>>
{
    public async Task<IReadOnlyList<TournamentPlayerDto>> Handle(GetTournamentPlayersQuery request, CancellationToken cancellationToken)
    {
        return await db.TournamentPlayers
            .Where(p => p.TournamentId == request.TournamentId)
            .OrderByDescending(p => p.Score)
            .Select(p => new TournamentPlayerDto(
                p.Id,
                p.UserId,
                p.User.UserName ?? string.Empty,
                p.User.Name,
                p.User.ImageUrl,
                p.Score,
                p.WinCount,
                p.LoseCount,
                p.MatchCount))
            .ToListAsync(cancellationToken);
    }
}
