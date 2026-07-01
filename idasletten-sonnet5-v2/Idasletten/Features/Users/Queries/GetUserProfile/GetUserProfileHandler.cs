using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries.GetUserProfile;

public class GetUserProfileHandler(IdaslettenDbContext db) : IRequestHandler<GetUserProfileQuery, UserProfileResult?>
{
    public async Task<UserProfileResult?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.TournamentPlayers)
            .ThenInclude(p => p.Tournament)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var tournaments = user.TournamentPlayers
            .OrderByDescending(p => p.Tournament.IsArchived ? 0 : 1)
            .ThenBy(p => p.Tournament.Name)
            .Select(p => new UserProfileTournamentStat(
                p.TournamentId,
                p.Tournament.Name,
                p.Tournament.ScoreSystem,
                p.Score,
                p.WinCount,
                p.LoseCount,
                p.MatchCount,
                p.PointsWon,
                p.PointsLost))
            .ToList();

        return new UserProfileResult(user.Id, user.UserName ?? string.Empty, user.Name, user.ImageUrl, tournaments);
    }
}
