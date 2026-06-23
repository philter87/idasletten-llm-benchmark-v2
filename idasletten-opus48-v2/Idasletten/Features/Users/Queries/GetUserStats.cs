using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record UserTournamentStat(
    Guid TournamentId, string TournamentName, double Score, int WinCount, int LoseCount, int MatchCount);

public record UserStats(
    Guid UserId, string Initials, string Name, string? Email, string? ImageUrl,
    int TotalWins, int TotalLosses, int TotalMatches,
    IReadOnlyList<UserTournamentStat> Tournaments);

public record GetUserStatsQuery(Guid UserId) : IRequest<UserStats?>;

public class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, UserStats?>
{
    private readonly AppDbContext _db;
    public GetUserStatsHandler(AppDbContext db) => _db = db;

    public async Task<UserStats?> Handle(GetUserStatsQuery q, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == q.UserId, ct);
        if (user is null) return null;

        var stats = await _db.TournamentPlayers
            .Where(p => p.UserId == q.UserId)
            .Include(p => p.Tournament)
            .OrderBy(p => p.Tournament.Name)
            .Select(p => new UserTournamentStat(
                p.TournamentId, p.Tournament.Name, p.Score, p.WinCount, p.LoseCount, p.MatchCount))
            .ToListAsync(ct);

        return new UserStats(
            user.Id, user.UserName!, user.Name, user.Email, user.ImageUrl,
            stats.Sum(s => s.WinCount), stats.Sum(s => s.LoseCount), stats.Sum(s => s.MatchCount),
            stats);
    }
}
