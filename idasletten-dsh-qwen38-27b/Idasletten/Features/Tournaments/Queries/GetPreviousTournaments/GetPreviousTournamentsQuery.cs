using Idasletten.Data;
using Idasletten.Features.Tournaments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetPreviousTournaments;

/// <summary>Tournaments that can act as a source (seed / add players): anything except the current one and its descendants.</summary>
public sealed record GetPreviousTournamentsQuery(Guid CurrentTournamentId) : IRequest<IReadOnlyList<TournamentCardDto>>;

public sealed class GetPreviousTournamentsQueryHandler : IRequestHandler<GetPreviousTournamentsQuery, IReadOnlyList<TournamentCardDto>>
{
    private readonly AppDbContext _db;

    public GetPreviousTournamentsQueryHandler(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<TournamentCardDto>> Handle(GetPreviousTournamentsQuery request, CancellationToken cancellationToken)
    {
        // Descendants of the current tournament (walk the child chain, bounded depth).
        var excluded = new List<Guid> { request.CurrentTournamentId };
        var frontier = new List<Guid> { request.CurrentTournamentId };
        for (var depth = 0; depth < 10 && frontier.Count > 0; depth++)
        {
            var next = await _db.Tournaments
                .Where(t => t.ParentTournamentId != null && frontier.Contains(t.ParentTournamentId.Value))
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
            frontier = next.Where(id => !excluded.Contains(id)).ToList();
            excluded.AddRange(frontier);
        }

        return await _db.Tournaments
            .Where(t => !excluded.Contains(t.Id))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TournamentCardDto(
                t.Id, t.Name, t.ScoreSystem, t.TeamSize, t.PointsToWin,
                t.IsPublic, t.IsArchived, t.ParentTournamentId != null, t.RoundNumber,
                _db.TournamentPlayers.Count(p => p.TournamentId == t.Id)))
            .ToListAsync(cancellationToken);
    }
}
