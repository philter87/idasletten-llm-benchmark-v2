using Idasletten.Features.Tournaments;
using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

public record ListTournamentPlayersQuery(Guid TournamentId) : IRequest<List<PlayerView>>;
public record GetPlayersFromTournamentQuery(Guid SourceTournamentId, Guid CurrentTournamentId)
    : IRequest<List<SeedPlayerView>>;

public class SeedPlayerView
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public double SourceScore { get; set; }
    public bool AlreadyInCurrent { get; set; }
}

public class PlayerQueryHandlers(IdaslettenDbContext db) :
    IRequestHandler<ListTournamentPlayersQuery, List<PlayerView>>,
    IRequestHandler<GetPlayersFromTournamentQuery, List<SeedPlayerView>>
{
    private readonly IdaslettenDbContext _db = db;

    public async Task<List<PlayerView>> Handle(ListTournamentPlayersQuery req, CancellationToken ct)
    {
        return await _db.TournamentPlayers.AsNoTracking()
            .Where(p => p.TournamentId == req.TournamentId)
            .OrderByDescending(p => p.Score).ThenByDescending(p => p.PointsWon - p.PointsLost)
            .Select(p => new PlayerView
            {
                Id = p.Id, UserId = p.UserId, Username = p.User!.Username, Name = p.User!.Name,
                ImageUrl = p.User!.ImageUrl, Score = p.Score, WinCount = p.WinCount, LoseCount = p.LoseCount,
                MatchCount = p.MatchCount, Lives = p.Lives, PointsWon = p.PointsWon, PointsLost = p.PointsLost,
                ScoreDiff = p.ScoreDiff
            }).ToListAsync(ct);
    }

    public async Task<List<SeedPlayerView>> Handle(GetPlayersFromTournamentQuery req, CancellationToken ct)
    {
        return await _db.TournamentPlayers.AsNoTracking()
            .Where(p => p.TournamentId == req.SourceTournamentId)
            .OrderByDescending(p => p.Score)
            .Select(p => new SeedPlayerView
            {
                UserId = p.UserId,
                Username = p.User!.Username,
                Name = p.User!.Name,
                SourceScore = p.Score,
                AlreadyInCurrent = _db.TournamentPlayers.Any(x => x.UserId == p.UserId && x.TournamentId == req.CurrentTournamentId)
            })
            .ToListAsync(ct);
    }
}