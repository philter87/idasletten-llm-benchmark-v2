using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Users.Queries;

public class UserStatsDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TotalMatches { get; set; }
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int TotalPointsWon { get; set; }
    public int TotalPointsLost { get; set; }
    public List<UserTournamentStatsDto> Tournaments { get; set; } = [];
}

public class UserTournamentStatsDto
{
    public Guid TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public double Score { get; set; }
    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }
}

public record GetUserStatsQuery(Guid UserId) : IRequest<UserStatsDto?>;

public class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, UserStatsDto?>
{
    private readonly Shared.Data.ApplicationDbContext _db;

    public GetUserStatsHandler(Shared.Data.ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserStatsDto?> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return null;

        var players = await _db.TournamentPlayers
            .AsNoTracking()
            .Where(p => p.UserId == request.UserId)
            .Include(p => p.Tournament)
            .ToListAsync(cancellationToken);

        return new UserStatsDto
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            ImageUrl = user.ImageUrl,
            TotalMatches = players.Sum(p => p.MatchCount),
            TotalWins = players.Sum(p => p.WinCount),
            TotalLosses = players.Sum(p => p.LoseCount),
            TotalPointsWon = players.Sum(p => p.PointsWon),
            TotalPointsLost = players.Sum(p => p.PointsLost),
            Tournaments = players.Select(p => new UserTournamentStatsDto
            {
                TournamentId = p.TournamentId,
                TournamentName = p.Tournament.Name,
                Score = p.Score,
                WinCount = p.WinCount,
                MatchCount = p.MatchCount,
                LoseCount = p.LoseCount
            }).ToList()
        };
    }
}
