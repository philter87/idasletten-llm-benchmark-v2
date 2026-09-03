using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries.GetUserStats;

public sealed record UserTournamentRowDto(Guid TournamentId, string TournamentName, ScoreSystem ScoreSystem,
    bool IsArchived, bool IsPublic, double Score, double ScoreDiff, int WinCount, int LoseCount,
    int MatchCount, int PointsWon, int PointsLost, int Lives, int? RoundNumber, bool IsChild);

public sealed record UserStatsDto(string Username, string Name, string? Email, string? ImageUrl,
    IReadOnlyList<UserTournamentRowDto> Tournaments,
    int TotalMatches, int TotalWins, int TotalLosses, int TotalPointsWon, int TotalPointsLost);

public sealed record GetUserStatsQuery(Guid UserId) : IRequest<UserStatsDto?>;

/// <summary>Cross-tournament stats for a single player.</summary>
public sealed class GetUserStatsQueryHandler : IRequestHandler<GetUserStatsQuery, UserStatsDto?>
{
    private readonly AppDbContext _db;

    public GetUserStatsQueryHandler(AppDbContext db) => _db = db;

    public async Task<UserStatsDto?> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null) return null;

        var rows = await _db.TournamentPlayers
            .Include(p => p.Tournament)
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.Tournament.CreatedAt)
            .Select(p => new UserTournamentRowDto(
                p.Tournament.Id, p.Tournament.Name, p.Tournament.ScoreSystem,
                p.Tournament.IsArchived, p.Tournament.IsPublic,
                p.Score, p.ScoreDiff, p.WinCount, p.LoseCount, p.MatchCount,
                p.PointsWon, p.PointsLost, p.Lives, p.Tournament.RoundNumber,
                p.Tournament.ParentTournamentId != null))
            .ToListAsync(cancellationToken);

        return new UserStatsDto(
            user.Username, user.Name, user.Email, user.ImageUrl,
            rows,
            rows.Sum(r => r.MatchCount),
            rows.Sum(r => r.WinCount),
            rows.Sum(r => r.LoseCount),
            rows.Sum(r => r.PointsWon),
            rows.Sum(r => r.PointsLost));
    }
}
