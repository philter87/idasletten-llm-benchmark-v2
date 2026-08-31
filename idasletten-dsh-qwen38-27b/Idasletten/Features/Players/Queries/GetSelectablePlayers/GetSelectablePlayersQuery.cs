using Idasletten.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Queries.GetSelectablePlayers;

public sealed record PlayerSelectDto(Guid TournamentPlayerId, Guid UserId, string Initials, string Name, bool InTournament);

public sealed record GetSelectablePlayersQuery(Guid TournamentId) : IRequest<IReadOnlyList<PlayerSelectDto>>;

/// <summary>All users of the tournament (for the "select from list" dialogs).</summary>
public sealed class GetSelectablePlayersQueryHandler : IRequestHandler<GetSelectablePlayersQuery, IReadOnlyList<PlayerSelectDto>>
{
    private readonly AppDbContext _db;

    public GetSelectablePlayersQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlayerSelectDto>> Handle(GetSelectablePlayersQuery request, CancellationToken cancellationToken)
    {
        var inTournament = await _db.TournamentPlayers
            .Where(p => p.TournamentId == request.TournamentId)
            .ToDictionaryAsync(p => p.UserId, p => p.Id, cancellationToken);

        var users = await _db.Users
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        return users.Select(u =>
        {
            var inT = inTournament.TryGetValue(u.Id, out var tpId);
            return new PlayerSelectDto(inT ? tpId : Guid.Empty, u.Id, u.Username, u.Name, inT);
        }).ToList();
    }
}
