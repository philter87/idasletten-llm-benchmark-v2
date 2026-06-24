using Idasletten.Shared.Entities;
using Idasletten.Shared.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record GetTournamentsQuery(bool IncludeArchived = false, bool IncludeChildren = false) : IRequest<List<Tournament>>;

public class GetTournamentsHandler : IRequestHandler<GetTournamentsQuery, List<Tournament>>
{
    private readonly AppDbContext _db;

    public GetTournamentsHandler(AppDbContext db) => _db = db;

    public async Task<List<Tournament>> Handle(GetTournamentsQuery request, CancellationToken ct)
    {
        var query = _db.Tournaments.AsQueryable();

        if (!request.IncludeArchived)
            query = query.Where(t => !t.IsArchived);

        if (!request.IncludeChildren)
            query = query.Where(t => t.ParentTournamentId == null);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }
}

public record GetPublicTournamentsQuery : IRequest<List<Tournament>>;

public class GetPublicTournamentsHandler : IRequestHandler<GetPublicTournamentsQuery, List<Tournament>>
{
    private readonly AppDbContext _db;

    public GetPublicTournamentsHandler(AppDbContext db) => _db = db;

    public async Task<List<Tournament>> Handle(GetPublicTournamentsQuery request, CancellationToken ct)
    {
        return await _db.Tournaments
            .Where(t => t.IsPublic && !t.IsArchived && t.ParentTournamentId == null)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }
}

public record GetTournamentByIdQuery(Guid Id) : IRequest<Tournament?>;

public class GetTournamentByIdHandler : IRequestHandler<GetTournamentByIdQuery, Tournament?>
{
    private readonly AppDbContext _db;

    public GetTournamentByIdHandler(AppDbContext db) => _db = db;

    public async Task<Tournament?> Handle(GetTournamentByIdQuery request, CancellationToken ct)
    {
        return await _db.Tournaments
            .Include(t => t.Players)
                .ThenInclude(p => p.User)
            .Include(t => t.SeedTournament)
            .Include(t => t.ParentTournament)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);
    }
}
