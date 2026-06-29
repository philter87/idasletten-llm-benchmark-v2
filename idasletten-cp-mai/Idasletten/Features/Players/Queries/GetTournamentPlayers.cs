using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

public class TournamentPlayerDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Initials { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public double Score { get; set; }
    public double ScoreDiff { get; set; }
    public int WinCount { get; set; }
    public int MatchCount { get; set; }
    public int LoseCount { get; set; }
    public int Lives { get; set; }
    public int PointsWon { get; set; }
    public int PointsLost { get; set; }
    public int GoalDifference => PointsWon - PointsLost;
}

public record GetTournamentPlayersQuery(Guid TournamentId) : IRequest<List<TournamentPlayerDto>>;

public class GetTournamentPlayersHandler : IRequestHandler<GetTournamentPlayersQuery, List<TournamentPlayerDto>>
{
    private readonly Shared.Data.ApplicationDbContext _db;

    public GetTournamentPlayersHandler(Shared.Data.ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<TournamentPlayerDto>> Handle(GetTournamentPlayersQuery request, CancellationToken cancellationToken)
    {
        var tournament = await _db.Tournaments
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);

        var players = await _db.TournamentPlayers
            .AsNoTracking()
            .Where(p => p.TournamentId == request.TournamentId)
            .Include(p => p.User)
            .ToListAsync(cancellationToken);

        var dtos = players.Select(p => new TournamentPlayerDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Initials = p.User.Username,
            Name = p.User.Name,
            ImageUrl = p.User.ImageUrl,
            Score = p.Score,
            ScoreDiff = p.ScoreDiff,
            WinCount = p.WinCount,
            MatchCount = p.MatchCount,
            LoseCount = p.LoseCount,
            Lives = p.Lives,
            PointsWon = p.PointsWon,
            PointsLost = p.PointsLost
        }).ToList();

        return tournament?.ScoreSystem switch
        {
            Tournaments.ScoreSystem.WinCount => dtos
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.GoalDifference)
                .ThenByDescending(p => p.WinCount)
                .ToList(),
            Tournaments.ScoreSystem.Lives => dtos
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.MatchCount)
                .ToList(),
            _ => dtos
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.GoalDifference)
                .ToList()
        };
    }
}
