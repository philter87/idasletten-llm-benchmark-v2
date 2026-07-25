using Idasletten.Features.Players.Events;
using Idasletten.Features.Scoring;
using Idasletten.Features.Users.Commands;
using Idasletten.Shared.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Idasletten.Features.Players.Commands;

/// <summary>
/// Adds a player to a tournament by initials. Initials that have never been used before also create
/// the user. Adding somebody who is already in the tournament is a no-op.
/// </summary>
public record AddPlayerToTournament(Guid TournamentId, string Initials, string? Name = null)
    : IRequest<Guid>;

public class AddPlayerToTournamentHandler(AppDbContext db, ISender sender, IPublisher publisher)
    : IRequestHandler<AddPlayerToTournament, Guid>
{
    public async Task<Guid> Handle(AddPlayerToTournament request, CancellationToken cancellationToken)
    {
        var tournament = await db.Tournaments
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == request.TournamentId, cancellationToken)
            ?? throw new ArgumentException("Unknown tournament.", nameof(request));

        var user = await sender.Send(new GetOrCreateUser(request.Initials, request.Name), cancellationToken);

        var existing = tournament.Players.FirstOrDefault(p => p.UserId == user.Id);
        if (existing is not null)
        {
            return existing.Id;
        }

        if (!tournament.HasRoomForMorePlayers(tournament.Players.Count))
        {
            throw new InvalidOperationException(
                $"{tournament.Name} is full ({tournament.MaxPlayerCount} players).");
        }

        var player = new TournamentPlayer
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            UserId = user.Id,
        };

        ScoreEngine.Reset(tournament, player);

        db.TournamentPlayers.Add(player);
        await db.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new PlayerAddedToTournament(tournament.Id, player.Id, user.Id, user.Initials),
            cancellationToken);

        return player.Id;
    }
}
