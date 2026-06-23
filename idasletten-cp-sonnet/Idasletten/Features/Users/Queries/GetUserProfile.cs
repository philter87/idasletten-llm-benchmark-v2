using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record UserTournamentStatDto(
    Guid TournamentId,
    string TournamentName,
    double Score,
    int WinCount,
    int LoseCount,
    int MatchCount,
    int PointsWon,
    int PointsLost);

public record UserProfileDto(UserDto User, IReadOnlyList<UserTournamentStatDto> TournamentStats);

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto?>;

public sealed class GetUserProfileHandler(AppDbContext db, IMediator mediator)
    : IRequestHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly AppDbContext _db = db;

    public async Task<UserProfileDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserDto(u.Id, u.Username, u.Name, u.Email, u.ImageUrl))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) return null;

        var stats = (await _db.TournamentPlayers
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .Include(p => p.Tournament)
            .Select(p => new UserTournamentStatDto(
                p.TournamentId,
                p.Tournament.Name,
                p.Score,
                p.WinCount,
                p.LoseCount,
                p.MatchCount,
                p.PointsWon,
                p.PointsLost))
            .ToListAsync(cancellationToken))
            .ToList();

        return new UserProfileDto(user, stats);
    }
}
