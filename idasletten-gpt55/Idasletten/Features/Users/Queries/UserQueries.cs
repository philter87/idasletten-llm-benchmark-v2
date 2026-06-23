using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record UserTournamentStats(string TournamentName, double Score, int Wins, int Losses, int Matches, int PointsWon, int PointsLost);
public record UserDetail(Guid Id, string Initials, string Name, string? Email, IReadOnlyList<UserTournamentStats> Stats);
public record GetUserDetailQuery(Guid UserId) : IRequest<UserDetail?>;

public class GetUserDetailHandler(IdaslettenDbContext db) : IRequestHandler<GetUserDetailQuery, UserDetail?>
{
    public async Task<UserDetail?> Handle(GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null) return null;
        var players = await db.TournamentPlayers.AsNoTracking().Include(p => p.Tournament).Where(p => p.UserId == request.UserId).ToListAsync(cancellationToken);
        var stats = players.OrderByDescending(p => p.Tournament.CreatedAt).Select(p => new UserTournamentStats(p.Tournament.Name, p.Score, p.WinCount, p.LoseCount, p.MatchCount, p.PointsWon, p.PointsLost)).ToList();
        return new UserDetail(user.Id, user.UserName, user.Name, user.Email, stats);
    }
}
