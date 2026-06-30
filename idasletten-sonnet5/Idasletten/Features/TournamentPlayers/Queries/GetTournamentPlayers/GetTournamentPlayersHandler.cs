using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.TournamentPlayers.Queries.GetTournamentPlayers;

public class GetTournamentPlayersHandler(IdaslettenDbContext db)
    : IRequestHandler<GetTournamentPlayersQuery, IReadOnlyList<TournamentPlayerDto>>
{
    public async Task<IReadOnlyList<TournamentPlayerDto>> Handle(GetTournamentPlayersQuery request, CancellationToken cancellationToken)
    {
        return await (
            from p in db.TournamentPlayers
            join u in db.Users on p.UserId equals u.Id
            where p.TournamentId == request.TournamentId
            orderby p.Score descending, (p.PointsWon - p.PointsLost) descending
            select new TournamentPlayerDto(
                p.Id, u.Id, u.UserName!, u.Name, u.ImageUrl,
                p.Score, p.WinCount, p.LoseCount, p.MatchCount, p.Lives,
                p.PointsWon, p.PointsLost, p.ScoreDiff)
        ).ToListAsync(cancellationToken);
    }
}
