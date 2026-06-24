using Idasletten.Features.Tournaments.Queries;
using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public class GetTournamentsHandler :
    IRequestHandler<GetTournamentsQuery, List<Tournament>>,
    IRequestHandler<GetPublicTournamentsQuery, List<Tournament>>,
    IRequestHandler<GetTournamentByIdQuery, Tournament?>
{
    private readonly AppDbContext _db;

    public GetTournamentsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Tournament>> Handle(GetTournamentsQuery query, CancellationToken ct)
    {
        var q = _db.Tournaments.AsQueryable();
        if (!query.IncludeArchived)
            q = q.Where(t => !t.IsArchived && t.ParentTournamentId == null);
        return await q.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task<List<Tournament>> Handle(GetPublicTournamentsQuery query, CancellationToken ct)
    {
        return await _db.Tournaments
            .Where(t => t.IsPublic && !t.IsArchived && t.ParentTournamentId == null)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Tournament?> Handle(GetTournamentByIdQuery query, CancellationToken ct)
    {
        return await _db.Tournaments
            .Include(t => t.Players).ThenInclude(p => p.User)
            .Include(t => t.Matches)
            .Include(t => t.SeedTournament)
            .Include(t => t.ParentTournament)
            .FirstOrDefaultAsync(t => t.Id == query.TournamentId, ct);
    }
}
