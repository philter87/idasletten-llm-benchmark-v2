using Idasletten.Data;
using Idasletten.Features.Tournaments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetPublicTournaments;

public sealed record GetPublicTournamentsQuery : IRequest<IReadOnlyList<TournamentCardDto>>;

public sealed class GetPublicTournamentsQueryHandler : IRequestHandler<GetPublicTournamentsQuery, IReadOnlyList<TournamentCardDto>>
{
    private readonly AppDbContext _db;

    public GetPublicTournamentsQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TournamentCardDto>> Handle(GetPublicTournamentsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Tournaments
            .Where(t => t.IsPublic && !t.IsArchived)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TournamentCardDto(
                t.Id, t.Name, t.ScoreSystem, t.TeamSize, t.PointsToWin,
                t.IsPublic, t.IsArchived, t.ParentTournamentId != null, t.RoundNumber,
                _db.TournamentPlayers.Count(p => p.TournamentId == t.Id)))
            .ToListAsync(cancellationToken);
    }
}
