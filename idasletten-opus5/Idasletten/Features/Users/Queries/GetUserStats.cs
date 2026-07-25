using Idasletten.Features.Scoring;
using Idasletten.Features.Tournaments;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public record GetUserStats(Guid UserId) : IRequest<UserStats?>;

public record UserStats(
    Guid UserId,
    string Initials,
    string DisplayName,
    string? Email,
    string? ImageUrl,
    int TournamentCount,
    int MatchCount,
    int WinCount,
    int LoseCount,
    int PointsWon,
    int PointsLost,
    IReadOnlyList<UserTournamentStats> Tournaments)
{
    public double WinRate => MatchCount == 0 ? 0 : (double)WinCount / MatchCount * 100;
    public int PointsDiff => PointsWon - PointsLost;
}

public record UserTournamentStats(
    Guid TournamentId,
    string TournamentName,
    ScoreSystem ScoreSystem,
    bool IsArchived,
    int? RoundNumber,
    DateTime CreatedUtc,
    int Rank,
    int PlayerCount,
    double Score,
    int MatchCount,
    int WinCount,
    int LoseCount,
    int PointsWon,
    int PointsLost);

public class GetUserStatsHandler(AppDbContext db) : IRequestHandler<GetUserStats, UserStats?>
{
    public async Task<UserStats?> Handle(GetUserStats request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var participations = await db.TournamentPlayers
            .AsNoTracking()
            .Include(p => p.Tournament)
            .Where(p => p.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        var tournamentIds = participations.Select(p => p.TournamentId).ToList();

        var allPlayers = await db.TournamentPlayers
            .AsNoTracking()
            .Where(p => tournamentIds.Contains(p.TournamentId))
            .ToListAsync(cancellationToken);

        var tournaments = new List<UserTournamentStats>();
        foreach (var participation in participations)
        {
            var field = allPlayers.Where(p => p.TournamentId == participation.TournamentId).ToList();
            var ranked = ScoreEngine.Rank(field).ToList();
            var rank = ranked.FindIndex(p => p.Id == participation.Id) + 1;

            tournaments.Add(new UserTournamentStats(
                participation.TournamentId,
                participation.Tournament.Name,
                participation.Tournament.ScoreSystem,
                participation.Tournament.IsArchived,
                participation.Tournament.RoundNumber,
                participation.Tournament.CreatedUtc,
                rank,
                field.Count,
                participation.Score,
                participation.MatchCount,
                participation.WinCount,
                participation.LoseCount,
                participation.PointsWon,
                participation.PointsLost));
        }

        return new UserStats(
            user.Id,
            user.Initials,
            user.DisplayName,
            user.Email,
            user.ImageUrl,
            participations.Count,
            participations.Sum(p => p.MatchCount),
            participations.Sum(p => p.WinCount),
            participations.Sum(p => p.LoseCount),
            participations.Sum(p => p.PointsWon),
            participations.Sum(p => p.PointsLost),
            tournaments.OrderByDescending(t => t.CreatedUtc).ThenBy(t => t.TournamentName).ToList());
    }
}
