using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public record GetPublicTournamentsQuery : IRequest<List<Tournament>>;

public class GetPublicTournamentsHandler : IRequestHandler<GetPublicTournamentsQuery, List<Tournament>>
{
    private readonly AppDbContext _db;

    public GetPublicTournamentsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Tournament>> Handle(GetPublicTournamentsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Tournaments
            .Where(t => t.IsPublic && !t.IsArchived && t.ParentTournamentId == null)
            .OrderByDescending(t => t.Name)
            .ToListAsync(cancellationToken);
    }
}
