using Idasletten.Data;
using Idasletten.Features.Tournaments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetAllTournaments;

/// <summary>Historical list. Child rounds are hidden unless IncludeChildren is set.</summary>
public sealed record GetAllTournamentsQuery(bool IncludeChildren = false) : IRequest<IReadOnlyList<TournamentCardDto>>;

public sealed class GetAllTournamentsQueryHandler : IRequestHandler<GetAllTournamentsQuery, IReadOnlyList<TournamentCardDto>>
{
    private readonly AppDbContext _db;

    public GetAllTournamentsQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TournamentCardDto>> Handle(GetAllTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tournaments.AsQueryable();
        if (!request.IncludeChildren)
            query = query.Where(t => t.ParentTournamentId == null);
        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TournamentCardDto(
                t.Id, t.Name, t.ScoreSystem, t.TeamSize, t.PointsToWin,
                t.IsPublic, t.IsArchived, t.ParentTournamentId != null, t.RoundNumber,
                _db.TournamentPlayers.Count(p => p.TournamentId == t.Id)))
            .ToListAsync(cancellationToken);
    }
}
