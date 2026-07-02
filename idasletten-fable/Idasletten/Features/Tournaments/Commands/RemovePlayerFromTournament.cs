using Idasletten.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Tournaments.Commands;

/// <summary>Removes a player from a tournament. Only allowed before they have played a match.</summary>
public record RemovePlayerFromTournamentCommand(Guid TournamentId, Guid UserId) : IRequest;

public record PlayerRemovedFromTournament(Guid TournamentId, Guid UserId) : INotification;

public class RemovePlayerFromTournamentHandler(AppDbContext db, IPublisher publisher)
    : IRequestHandler<RemovePlayerFromTournamentCommand>
{
    public async Task Handle(RemovePlayerFromTournamentCommand request, CancellationToken ct)
    {
        var player = await db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.TournamentId == request.TournamentId && p.UserId == request.UserId, ct);
        if (player is null)
            return;

        if (player.MatchCount > 0)
            throw new InvalidOperationException("Spilleren har allerede spillet kampe og kan ikke fjernes.");

        db.TournamentPlayers.Remove(player);
        await db.SaveChangesAsync(ct);

        await publisher.Publish(new PlayerRemovedFromTournament(request.TournamentId, request.UserId), ct);
    }
}
