using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public class TournamentListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPublic { get; set; }
    public ScoreSystem ScoreSystem { get; set; }
    public int PlayerCount { get; set; }
    public int MatchCount { get; set; }
}

public record ListTournamentsQuery(bool IncludeHistorical = false) : IRequest<List<TournamentListItemDto>>;

public class ListTournamentsHandler : IRequestHandler<ListTournamentsQuery, List<TournamentListItemDto>>
{
    private readonly Shared.Data.ApplicationDbContext _db;

    public ListTournamentsHandler(Shared.Data.ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<TournamentListItemDto>> Handle(ListTournamentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tournaments.AsNoTracking();

        if (!request.IncludeHistorical)
        {
            query = query.Where(t => !t.IsArchived && t.IsPublic);
        }

        query = query.Where(t => t.ParentTournamentId == null);

        var tournaments = await query
            .OrderByDescending(t => t.RoundNumber)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var ids = tournaments.Select(t => t.Id).ToList();
        var playerCounts = await _db.TournamentPlayers
            .Where(p => ids.Contains(p.TournamentId))
            .GroupBy(p => p.TournamentId)
            .Select(g => new { TournamentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TournamentId, x => x.Count, cancellationToken);

        var matchCounts = await _db.TournamentMatches
            .Where(m => ids.Contains(m.TournamentId))
            .GroupBy(m => m.TournamentId)
            .Select(g => new { TournamentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TournamentId, x => x.Count, cancellationToken);

        return tournaments.Select(t => new TournamentListItemDto
        {
            Id = t.Id,
            Name = t.Name,
            RoundNumber = t.RoundNumber,
            IsArchived = t.IsArchived,
            IsPublic = t.IsPublic,
            ScoreSystem = t.ScoreSystem,
            PlayerCount = playerCounts.GetValueOrDefault(t.Id),
            MatchCount = matchCounts.GetValueOrDefault(t.Id)
        }).ToList();
    }
}
