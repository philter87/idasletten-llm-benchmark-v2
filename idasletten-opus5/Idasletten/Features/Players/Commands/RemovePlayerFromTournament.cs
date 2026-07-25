using Idasletten.Features.Players.Events;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

/// <summary>Removes a player that has not played any match yet.</summary>
public record RemovePlayerFromTournament(Guid TournamentId, Guid TournamentPlayerId) : IRequest<Unit>;

public class RemovePlayerFromTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<RemovePlayerFromTournament, Unit>
{
    public async Task<Unit> Handle(RemovePlayerFromTournament request, CancellationToken cancellationToken)
    {
        var player = await db.TournamentPlayers
            .Include(p => p.TeamMemberships)
            .FirstOrDefaultAsync(
                p => p.Id == request.TournamentPlayerId && p.TournamentId == request.TournamentId,
                cancellationToken);

        if (player is null)
        {
            return Unit.Value;
        }

        if (player.TeamMemberships.Count > 0)
        {
            throw new InvalidOperationException(
                "The player is already on a team in this tournament and cannot be removed.");
        }

        db.TournamentPlayers.Remove(player);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new PlayerRemovedFromTournament(request.TournamentId, request.TournamentPlayerId),
            cancellationToken);

        return Unit.Value;
    }
}
