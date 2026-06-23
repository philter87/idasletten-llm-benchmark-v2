using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries;

public record PlayerRow(Guid UserId, string Initials, string Name, string? ImageUrl, double Score, int MatchCount);

public record GetPlayersQuery(Guid TournamentId) : IRequest<List<PlayerRow>>;

public class GetPlayersHandler : IRequestHandler<GetPlayersQuery, List<PlayerRow>>
{
    private readonly AppDbContext _db;
    public GetPlayersHandler(AppDbContext db) => _db = db;

    public Task<List<PlayerRow>> Handle(GetPlayersQuery q, CancellationToken ct) =>
        _db.TournamentPlayers
            .Where(p => p.TournamentId == q.TournamentId)
            .Include(p => p.User)
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.User.UserName)
            .Select(p => new PlayerRow(p.UserId, p.User.UserName!, p.User.Name, p.User.ImageUrl, p.Score, p.MatchCount))
            .ToListAsync(ct);
}
