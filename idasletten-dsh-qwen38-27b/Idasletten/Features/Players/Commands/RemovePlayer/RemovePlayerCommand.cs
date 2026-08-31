using Idasletten.Data;
using Idasletten.Features.Common;
using Idasletten.Features.Players.Events;
using Idasletten.Features.Tournaments;
using Idasletten.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands.RemovePlayer;

public sealed record RemovePlayerCommand(Guid TournamentId, Guid TournamentPlayerId) : IRequest<Unit>;

public sealed class RemovePlayerCommandHandler : IRequestHandler<RemovePlayerCommand, Unit>
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;

    public RemovePlayerCommandHandler(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(RemovePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await _db.TournamentPlayers
            .FirstOrDefaultAsync(p => p.Id == request.TournamentPlayerId && p.TournamentId == request.TournamentId, cancellationToken)
            ?? throw new FeatureException("Player not found in this tournament.");
        if (player.MatchCount > 0)
            throw new FeatureException("This player has played matches and can no longer be removed.");
        var t = await _db.Tournaments.FirstOrDefaultAsync(x => x.Id == request.TournamentId, cancellationToken);
        if (t is not null && t.IsArchived)
            throw new FeatureException("This tournament is archived.");

        _db.TournamentPlayers.Remove(player);
        await _db.SaveChangesAsync(cancellationToken);
        await _publisher.Publish(new PlayerRemoved(request.TournamentId, request.TournamentPlayerId), cancellationToken);
        return Unit.Value;
    }
}
