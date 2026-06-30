using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries.GetUser;

public class GetUserHandler(IdaslettenDbContext db) : IRequestHandler<GetUserQuery, UserDetailDto?>
{
    public async Task<UserDetailDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null) return null;

        var tournaments = await (
            from p in db.TournamentPlayers
            join t in db.Tournaments on p.TournamentId equals t.Id
            where p.UserId == request.UserId
            orderby t.CreatedAtUtc descending
            select new UserTournamentStatsDto(
                t.Id, t.Name, p.Score, p.WinCount, p.LoseCount, p.MatchCount, p.PointsWon, p.PointsLost)
        ).ToListAsync(cancellationToken);

        return new UserDetailDto(user.Id, user.UserName!, user.Name, user.ImageUrl, tournaments);
    }
}
