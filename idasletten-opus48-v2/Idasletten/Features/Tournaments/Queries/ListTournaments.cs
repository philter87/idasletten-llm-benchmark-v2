using Idasletten.Data;
using Idasletten.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record TournamentListItem(
    Guid Id, string Name, ScoreSystem ScoreSystem, bool IsPublic, bool IsArchived,
    int PlayerCount, int? RoundNumber, Guid? ParentTournamentId);

/// <summary>Public, non-archived, top-level tournaments shown on the home page.</summary>
public record ListPublicTournamentsQuery : IRequest<List<TournamentListItem>>;

/// <summary>All historical tournaments. By default child rounds are excluded.</summary>
public record ListTournamentsQuery(bool IncludeChildren = false) : IRequest<List<TournamentListItem>>;

public class ListTournamentsHandler :
    IRequestHandler<ListPublicTournamentsQuery, List<TournamentListItem>>,
    IRequestHandler<ListTournamentsQuery, List<TournamentListItem>>
{
    private readonly AppDbContext _db;
    public ListTournamentsHandler(AppDbContext db) => _db = db;

    public Task<List<TournamentListItem>> Handle(ListPublicTournamentsQuery q, CancellationToken ct) =>
        Project(_db.Tournaments.Where(t => t.IsPublic && !t.IsArchived && t.ParentTournamentId == null)).ToListAsync(ct);

    public Task<List<TournamentListItem>> Handle(ListTournamentsQuery q, CancellationToken ct)
    {
        var query = _db.Tournaments.AsQueryable();
        if (!q.IncludeChildren)
            query = query.Where(t => t.ParentTournamentId == null);
        return Project(query).ToListAsync(ct);
    }

    private static IQueryable<TournamentListItem> Project(IQueryable<Tournament> query) =>
        query.OrderBy(t => t.Name)
            .Select(t => new TournamentListItem(
                t.Id, t.Name, t.ScoreSystem, t.IsPublic, t.IsArchived,
                t.Players.Count, t.RoundNumber, t.ParentTournamentId));
}
