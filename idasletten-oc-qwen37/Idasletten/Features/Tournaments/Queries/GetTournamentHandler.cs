using Idasletten.Data;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

public class GetTournamentHandler : IRequestHandler<GetTournamentQuery, Tournament?>
{
    private readonly AppDbContext _db;

    public GetTournamentHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Tournament?> Handle(GetTournamentQuery request, CancellationToken cancellationToken)
    {
        return await _db.Tournaments
            .Include(t => t.Players)
                .ThenInclude(p => p.User)
            .Include(t => t.Teams)
                .ThenInclude(t => t.Players)
            .Include(t => t.Matches)
                .ThenInclude(m => m.TeamResults)
                    .ThenInclude(r => r.Team)
                        .ThenInclude(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
    }
}
