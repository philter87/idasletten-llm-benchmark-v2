using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries.GetTournament;

public class GetTournamentHandler(IdaslettenDbContext db) : IRequestHandler<GetTournamentQuery, TournamentDto?>
{
    public async Task<TournamentDto?> Handle(GetTournamentQuery request, CancellationToken cancellationToken)
    {
        return await db.Tournaments
            .Where(t => t.Id == request.TournamentId)
            .Select(t => new TournamentDto(
                t.Id, t.Name, t.TeamSize, t.PointsToWin, t.ScoreSystem, t.MaxPlayerCount,
                t.IsArchived, t.IsPublic, t.SeedTournamentId, t.ParentTournamentId, t.RoundNumber,
                db.TournamentPlayers.Count(p => p.TournamentId == t.Id)))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
