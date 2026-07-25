using Idasletten.Features.Scoring;
using Idasletten.Features.Tournaments;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

/// <summary>The ranked scoreboard of a tournament.</summary>
public record GetScoreboard(Guid TournamentId) : IRequest<IReadOnlyList<ScoreboardRow>>;

public record ScoreboardRow(
    int Rank,
    Guid TournamentPlayerId,
    Guid UserId,
    string Initials,
    string DisplayName,
    string? ImageUrl,
    double Score,
    double ScoreDiff,
    int WinCount,
    int LoseCount,
    int DrawCount,
    int MatchCount,
    int PointsWon,
    int PointsLost,
    int Lives,
    bool IsKnockedOut)
{
    public int PointsDiff => PointsWon - PointsLost;
}

public class GetScoreboardHandler(AppDbContext db)
    : IRequestHandler<GetScoreboard, IReadOnlyList<ScoreboardRow>>
{
    public async Task<IReadOnlyList<ScoreboardRow>> Handle(
        GetScoreboard request, CancellationToken cancellationToken)
    {
        var scoreSystem = await db.Tournaments
            .Where(t => t.Id == request.TournamentId)
            .Select(t => (ScoreSystem?)t.ScoreSystem)
            .FirstOrDefaultAsync(cancellationToken);

        if (scoreSystem is null)
        {
            return [];
        }

        var players = await db.TournamentPlayers
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.TournamentId == request.TournamentId)
            .ToListAsync(cancellationToken);

        return ScoreEngine.Rank(players)
            .Select((player, index) => new ScoreboardRow(
                index + 1,
                player.Id,
                player.UserId,
                player.User.Initials,
                player.User.DisplayName,
                player.User.ImageUrl,
                player.Score,
                player.ScoreDiff,
                player.WinCount,
                player.LoseCount,
                player.DrawCount,
                player.MatchCount,
                player.PointsWon,
                player.PointsLost,
                player.Lives,
                player.IsKnockedOut(scoreSystem.Value)))
            .ToList();
    }
}
