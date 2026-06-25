using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

public class GetPlayerStatsHandler : IRequestHandler<GetPlayerStatsQuery, PlayerStatsDto?>
{
    private readonly AppDbContext _db;

    public GetPlayerStatsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PlayerStatsDto?> Handle(GetPlayerStatsQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.TournamentPlayers)
                .ThenInclude(tp => tp.Tournament)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return null;

        var stats = new PlayerStatsDto
        {
            UserId = user.Id,
            Username = user.Username,
            Name = user.Name,
            ImageUrl = user.ImageUrl,
            TotalMatches = user.TournamentPlayers.Sum(tp => tp.MatchCount),
            TotalWins = user.TournamentPlayers.Sum(tp => tp.WinCount),
            TotalLosses = user.TournamentPlayers.Sum(tp => tp.LoseCount),
            TotalPointsWon = user.TournamentPlayers.Sum(tp => tp.PointsWon),
            TotalPointsLost = user.TournamentPlayers.Sum(tp => tp.PointsLost)
        };

        foreach (var tp in user.TournamentPlayers)
        {
            stats.Tournaments.Add(new TournamentStatsDto
            {
                TournamentId = tp.TournamentId,
                TournamentName = tp.Tournament.Name,
                Matches = tp.MatchCount,
                Wins = tp.WinCount,
                Losses = tp.LoseCount,
                Score = tp.Score
            });
        }

        return stats;
    }
}
