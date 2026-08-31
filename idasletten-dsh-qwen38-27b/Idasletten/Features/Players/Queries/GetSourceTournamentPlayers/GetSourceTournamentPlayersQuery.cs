using Idasletten.Data;
using Idasletten.Features.Players.Queries.GetSelectablePlayers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries.GetSourceTournamentPlayers;

/// <summary>Players of a source (previous) tournament ordered by score there, for the "add based on previous tournament" section.</summary>
public sealed record GetSourceTournamentPlayersQuery(Guid SourceTournamentId, Guid CurrentTournamentId) : IRequest<IReadOnlyList<PlayerSelectDto>?>;

public sealed class GetSourceTournamentPlayersQueryHandler : IRequestHandler<GetSourceTournamentPlayersQuery, IReadOnlyList<PlayerSelectDto>?>
{
    private readonly AppDbContext _db;

    public GetSourceTournamentPlayersQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlayerSelectDto>?> Handle(GetSourceTournamentPlayersQuery request, CancellationToken cancellationToken)
    {
        var exists = await _db.Tournaments.AnyAsync(t => t.Id == request.SourceTournamentId, cancellationToken);
        if (!exists) return null;

        var source = await _db.TournamentPlayers
            .Include(p => p.User)
            .Where(p => p.TournamentId == request.SourceTournamentId)
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .ThenBy(p => p.User.Username)
            .ToListAsync(cancellationToken);

        var current = await _db.TournamentPlayers
            .Where(p => p.TournamentId == request.CurrentTournamentId)
            .ToDictionaryAsync(p => p.UserId, p => p.Id, cancellationToken);

        return source.Select(p =>
        {
            var inT = current.TryGetValue(p.UserId, out var tpId);
            return new PlayerSelectDto(inT ? tpId : Guid.Empty, p.UserId, p.User.Username, p.User.Name, inT);
        }).ToList();
    }
}
