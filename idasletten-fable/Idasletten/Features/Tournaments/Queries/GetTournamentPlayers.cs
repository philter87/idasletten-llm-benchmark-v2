using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Queries;

/// <summary>
/// Players of a tournament plus, when a seed tournament is selected, that
/// tournament's players ordered by their score there (for the +/- add list).
/// </summary>
public record GetTournamentPlayersQuery(Guid TournamentId, Guid? SeedTournamentId = null) : IRequest<TournamentPlayersResult?>;

public record TournamentPlayersResult(
    Tournament Tournament,
    List<TournamentPlayer> Players,
    Tournament? SeedTournament,
    List<TournamentPlayer> SeedPlayers,
    List<Tournament> AvailableSeedTournaments);

public class GetTournamentPlayersHandler(AppDbContext db) : IRequestHandler<GetTournamentPlayersQuery, TournamentPlayersResult?>
{
    public async Task<TournamentPlayersResult?> Handle(GetTournamentPlayersQuery request, CancellationToken ct)
    {
        var tournament = await db.Tournaments.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, ct);
        if (tournament is null)
            return null;

        var players = await db.TournamentPlayers.AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.TournamentId == tournament.Id)
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.PointsWon - p.PointsLost)
            .ToListAsync(ct);

        var seedId = tournament.SeedTournamentId ?? request.SeedTournamentId;
        Tournament? seedTournament = null;
        var seedPlayers = new List<TournamentPlayer>();
        if (seedId is Guid id)
        {
            seedTournament = await db.Tournaments.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (seedTournament is not null)
            {
                seedPlayers = await db.TournamentPlayers.AsNoTracking()
                    .Include(p => p.User)
                    .Where(p => p.TournamentId == id)
                    .OrderByDescending(p => p.Score)
                    .ThenByDescending(p => p.PointsWon - p.PointsLost)
                    .ToListAsync(ct);
            }
        }

        var availableSeeds = await db.Tournaments.AsNoTracking()
            .Where(t => t.Id != tournament.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return new TournamentPlayersResult(tournament, players, seedTournament, seedPlayers, availableSeeds);
    }
}
