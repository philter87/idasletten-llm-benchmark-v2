using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

/// <summary>Cross-tournament stats for a single player.</summary>
public record GetUserStatsQuery(Guid UserId) : IRequest<UserStatsResult?>;

public record UserStatsResult(User User, List<UserTournamentStats> Tournaments)
{
    public int TotalMatches => Tournaments.Sum(t => t.Player.MatchCount);
    public int TotalWins => Tournaments.Sum(t => t.Player.WinCount);
    public int TotalLosses => Tournaments.Sum(t => t.Player.LoseCount);
    public int TotalPointsWon => Tournaments.Sum(t => t.Player.PointsWon);
    public int TotalPointsLost => Tournaments.Sum(t => t.Player.PointsLost);
}

public record UserTournamentStats(Tournament Tournament, TournamentPlayer Player);

public class GetUserStatsHandler(AppDbContext db) : IRequestHandler<GetUserStatsQuery, UserStatsResult?>
{
    public async Task<UserStatsResult?> Handle(GetUserStatsQuery request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([request.UserId], ct);
        if (user is null)
            return null;

        var stats = await (
                from player in db.TournamentPlayers
                join tournament in db.Tournaments on player.TournamentId equals tournament.Id
                where player.UserId == request.UserId
                orderby tournament.CreatedAt descending
                select new { tournament, player })
            .ToListAsync(ct);

        return new UserStatsResult(user, stats.Select(s => new UserTournamentStats(s.tournament, s.player)).ToList());
    }
}
