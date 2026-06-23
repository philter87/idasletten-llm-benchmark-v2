using Idasletten.Data;
using Idasletten.Features.Matches;
using Idasletten.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record ScoreboardRow(
    Guid UserId, string Initials, string Name, string? ImageUrl,
    double Score, double ScoreDiff, int WinCount, int LoseCount, int MatchCount,
    int Lives, int PointsWon, int PointsLost);

public record TournamentDetail(
    Guid Id, string Name, ScoreSystem ScoreSystem, int TeamSize, int PointsToWin,
    bool IsPublic, bool IsArchived, int? MaxPlayerCount, int? RoundNumber,
    Guid? SeedTournamentId, Guid? ParentTournamentId,
    IReadOnlyList<ScoreboardRow> Scoreboard,
    IReadOnlyList<MatchView> PlannedMatches,
    IReadOnlyList<MatchView> RecentMatches);

public record GetTournamentDetailQuery(Guid TournamentId) : IRequest<TournamentDetail?>;

public class GetTournamentDetailHandler : IRequestHandler<GetTournamentDetailQuery, TournamentDetail?>
{
    private readonly AppDbContext _db;
    public GetTournamentDetailHandler(AppDbContext db) => _db = db;

    public async Task<TournamentDetail?> Handle(GetTournamentDetailQuery q, CancellationToken ct)
    {
        var t = await _db.Tournaments.FirstOrDefaultAsync(x => x.Id == q.TournamentId, ct);
        if (t is null) return null;

        var scoreboard = await _db.TournamentPlayers
            .Where(p => p.TournamentId == t.Id)
            .Include(p => p.User)
            // Score desc, then goal difference as the universal tie-breaker.
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .ThenBy(p => p.User.UserName)
            .Select(p => new ScoreboardRow(
                p.UserId, p.User.UserName!, p.User.Name, p.User.ImageUrl,
                p.Score, p.ScoreDiff, p.WinCount, p.LoseCount, p.MatchCount,
                p.Lives, p.PointsWon, p.PointsLost))
            .ToListAsync(ct);

        var matches = await MatchProjection.LoadAsync(_db, t.Id, ct);

        var planned = matches
            .Where(m => m.State == MatchState.Planned)
            .OrderBy(m => m.Order).Take(5).ToList();

        var recent = matches
            .Where(m => m.State == MatchState.Done)
            .OrderByDescending(m => m.Order).Take(5).ToList();

        return new TournamentDetail(
            t.Id, t.Name, t.ScoreSystem, t.TeamSize, t.PointsToWin,
            t.IsPublic, t.IsArchived, t.MaxPlayerCount, t.RoundNumber,
            t.SeedTournamentId, t.ParentTournamentId,
            scoreboard, planned, recent);
    }
}
