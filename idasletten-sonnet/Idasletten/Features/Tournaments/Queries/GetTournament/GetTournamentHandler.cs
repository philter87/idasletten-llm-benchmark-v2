using Idasletten.Shared.Data;
using Idasletten.Shared.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetTournament;

public class GetTournamentHandler(AppDbContext db) : IRequestHandler<GetTournamentQuery, Tournament?>
{
    public Task<Tournament?> Handle(GetTournamentQuery request, CancellationToken cancellationToken)
        => db.Tournaments
             .Include(t => t.Players).ThenInclude(p => p.User)
             .Include(t => t.Matches).ThenInclude(m => m.TeamResults).ThenInclude(r => r.Team).ThenInclude(t => t.Players).ThenInclude(p => p.User)
             .Include(t => t.SeedTournament)
             .Include(t => t.ParentTournament)
             .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken);
}
